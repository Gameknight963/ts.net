using System.Diagnostics;

try { Run(); return 0; }
catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }

static void Run()
{
    string root = Directory.GetCurrentDirectory();
    string fixture = Path.Combine(root, "artifacts", "project config tests");
    string compiler = Path.Combine(root, "compiler/bin/Release/net10.0/tsnetc.dll");
    string frontend = Path.Combine(root, "compiler/bin/Release/net10.0", OperatingSystem.IsWindows() ? "tsnet-frontend.exe" : "tsnet-frontend");
    void Write(string path, string text) {
        path = Path.Combine(fixture, path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }
    Write("package.json", "{\"type\":\"commonjs\"}");
    Write("shared/base.json", """
        {
          // Paths and type roots are relative to this inherited config.
          "compilerOptions": {
            "strict": true, "target": "esnext", "module": "nodenext",
            "moduleResolution": "nodenext", "experimentalDecorators": true,
            "exactOptionalPropertyTypes": true,
            "paths": {"@domain/*": ["./domain/*"]},
            "typeRoots": ["./types"], "types": ["project-env"],
          }
        }
        """);
    Write("shared/types/project-env/index.d.ts", "type ProjectTag = 'project';");
    Write("shared/domain/model.ts", "export class Box<T> { constructor(public value: T) {} }");
    Write("app/tsconfig.json", """
        {"extends":"../shared/base.json", "include":["src/**/*.ts"], "exclude":["src/excluded.ts"]}
        """);
    Write("app/src/excluded.ts", "const excluded: number = 'not checked';");
    // Keep all files owned by this test deterministic across repeated runs.
    Write("app/src/checked.ts", "export const checked = 1;");
    Write("app/src/main.ts", """
        import {Box} from '@domain/model';
        function mark(_target: object, key: string): void { console.log('decorator', key); }
        class Entry extends Box<number> {
          @mark double(): number { return this.value * 2; }
        }
        const tag: ProjectTag = 'project';
        const entry = new Entry(21);
        console.log(tag, entry.value, entry.double(), entry instanceof Box);
        """);
    string entry = Path.Combine(fixture, "app/src/main.ts");
    string config = Path.Combine(fixture, "app/tsconfig.json");
    string app = Path.Combine(fixture, "App.dll");
    string[] Compile(params string[] options) => new[] { compiler }.Concat(options).Concat(new[] {entry, app}).ToArray();
    RequireSuccess(Execute(root, "dotnet", Compile("--project", config)));
    File.Copy(Path.Combine(root, "runtime/bin/Release/net10.0/Tsnet.Runtime.dll"), Path.Combine(fixture, "Tsnet.Runtime.dll"), true);
    Write("App.runtimeconfig.json", """{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}""");
    string actual = RequireSuccess(Execute(root, "dotnet", app));
    string node = Path.Combine(fixture, "node");
    string[] tscArgs = ["--project", config, "--outDir", node, "--rootDir", fixture];
    RequireSuccess(OperatingSystem.IsWindows()
        ? Execute(root, "cmd", new[] { "/c", "tsc.cmd" }.Concat(tscArgs).ToArray())
        : Execute(root, "tsc", tscArgs));
    // TypeScript preserves paths aliases in JS output. Give Node the same
    // module through an ordinary package; ts.net must resolve the TS source.
    Write("node/node_modules/@domain/model/index.js", "module.exports = require('../../../shared/domain/model.js');");
    string expected = RequireSuccess(Execute(root, "node", Path.Combine(node, "app/src/main.js")));
    if (actual != expected || actual != "decorator double\nproject 21 42 true\n")
        throw new Exception($"Project output differs from Node:\n{actual}\n{expected}");
    RequireSuccess(Execute(root, "dotnet", Compile("-p", Path.GetDirectoryName(config)!)));
    string inspect = RequireSuccess(Execute(root, frontend, "--project", config, "--inspect", entry, "entry"));
    if (!inspect.Contains("Entry")) throw new Exception("Frontend project inspection failed");
    Console.WriteLine("PASS project aliases, inherited options and type roots, decorators, directory paths, and Node output");

    void Reject(string name, string expectedError, params string[] options) {
        Write("App.dll", "sentinel");
        var result = Execute(root, "dotnet", Compile(options));
        if (result.Code == 0 || !result.Error.Contains(expectedError))
            throw new Exception($"Expected {name} rejection ({expectedError}): {result}");
        if (File.ReadAllText(app) != "sentinel") throw new Exception($"{name} overwrote output after failure");
    }
    Reject("missing config", "5083", "--project", Path.Combine(fixture, "absent.json"));
    Write("bad.json", "{broken");
    Reject("invalid JSON", "diagnostics", "--project", Path.Combine(fixture, "bad.json"));
    Write("bad-option.json", "{\"compilerOptions\":{\"notARealOption\":true}}");
    Reject("invalid option", "5023", "--project", Path.Combine(fixture, "bad-option.json"));
    Write("bad-extends.json", "{\"extends\":\"./missing-base.json\"}");
    Reject("missing inherited config", "5083", "--project", Path.Combine(fixture, "bad-extends.json"));
    try {
        Write("app/src/checked.ts", "const invalid: number = null;");
        Reject("included sibling and inherited strict mode", "2322", "--project", config);
        Write("app/src/checked.ts", "const invalid: {value?: number} = {value: undefined};");
        Reject("exact optional property types", "2375", "--project", config);
    }
    finally { Write("app/src/checked.ts", "export const checked = 1;"); }
    Reject("entry outside project", "did not load the input file", "--project", Path.Combine(fixture, "shared/base.json"));
    Reject("duplicate project option", "--project requires", "--project", config, "-p", config);
    var missing = Execute(root, "dotnet", compiler, "--project");
    if (missing.Code == 0 || !missing.Error.Contains("--project requires")) throw new Exception("Missing project argument accepted");
    Console.WriteLine("PASS project diagnostics and output preservation");
}

static string RequireSuccess((int Code, string Output, string Error) result)
{
    if (result.Code != 0) throw new Exception($"Child process failed ({result.Code}): {result.Output}\n{result.Error}");
    return result.Output;
}

static (int Code, string Output, string Error) Execute(string directory, string command, params string[] args)
{
    var start = new ProcessStartInfo(command) {
        WorkingDirectory = directory, UseShellExecute = false, CreateNoWindow = true,
        RedirectStandardOutput = true, RedirectStandardError = true
    };
    foreach (string argument in args) start.ArgumentList.Add(argument);
    using var process = Process.Start(start) ?? throw new Exception($"Cannot start {command}");
    var output = process.StandardOutput.ReadToEndAsync();
    var error = process.StandardError.ReadToEndAsync();
    if (!process.WaitForExit(120_000)) { process.Kill(true); process.WaitForExit(); throw new Exception($"Timed out: {command}"); }
    return (process.ExitCode, output.GetAwaiter().GetResult().Replace("\r\n", "\n"), error.GetAwaiter().GetResult().Replace("\r\n", "\n"));
}
