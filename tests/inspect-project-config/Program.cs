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

    string CheckProject(string directory)
    {
        string project = Path.Combine(fixture, directory, "tsconfig.json");
        string source = Path.Combine(fixture, directory, "main.ts");
        string output = Path.Combine(fixture, directory, "App.dll");
        RequireSuccess(Execute(root, "dotnet", compiler, "--project", project, source, output));
        File.Copy(Path.Combine(fixture, "Tsnet.Runtime.dll"), Path.Combine(fixture, directory, "Tsnet.Runtime.dll"), true);
        File.Copy(Path.Combine(fixture, "App.runtimeconfig.json"), Path.Combine(fixture, directory, "App.runtimeconfig.json"), true);
        string actual = RequireSuccess(Execute(root, "dotnet", output));
        string[] arguments = ["--project", project, "--outDir", Path.Combine(fixture, directory, "node")];
        RequireSuccess(OperatingSystem.IsWindows()
            ? Execute(root, "cmd", new[] {"/c", "tsc.cmd"}.Concat(arguments).ToArray())
            : Execute(root, "tsc", arguments));
        string expected = RequireSuccess(Execute(root, "node", Path.Combine(fixture, directory, "node/main.js")));
        if (actual != expected) throw new Exception($"{directory} output differs from Node:\nActual:\n{actual}\nExpected:\n{expected}");
        return actual;
    }
    Write("defaults/tsconfig.json", """{"compilerOptions":{"strict":false,"typeRoots":["./types"],"types":["*"]},"files":["main.ts"]}""");
    Write("defaults/types/default-env/index.d.ts", "type DefaultTag = 'defaults';");
    Write("defaults/main.ts", "function echo(value) {return value;} const tag: DefaultTag = 'defaults'; console.log(echo(tag));");
    if (CheckProject("defaults") != "defaults\n") throw new Exception("Config defaults were overridden");
    Console.WriteLine("PASS config strict override and ambient type discovery");

    string fields = """
        const events: string[] = [];
        function init(label: string, value: number): number {events.push(label); return value;}
        const key = 'computed' as const;
        class Parent {
          set value(value: number) {events.push('setter:' + value);}
          get value(): number {return 30;}
          set parameter(value: number) {
            events.push('parameter:' + value);
            if (typeof (this as any).snapshot === 'function') events.push('early:' + (this as any).snapshot());
          }
          get parameter(): number {return 40;}
          static set flag(value: number) {events.push('static setter:' + value);}
          static get flag(): number {return 50;}
          constructor() {events.push('parent');}
        }
        class Child extends (Parent as any) {
          value = init('value', 7);
          observed = (this as any).parameter;
          untouched!: number;
          [key] = init('computed', 8);
          static flag = init('static', 9);
          static absent: number;
          #secret = init('private', 11);
          accessor automatic = init('auto', 12);
          arrow = () => this.value;
          expression = function() {return 13;};
          klass = class {};
          static callback = () => 1;
          constructor(public parameter: number = init('default', 4)) {super(); events.push('body:' + this.parameter);}
          read(): number {return this.#secret;}
          snapshot(): string {return String(this.#secret) + ':' + String(this.automatic);}
        }
        console.log('definition', events.join(','), Object.hasOwn(Child, 'flag'), Object.hasOwn(Child, 'absent'), Child.flag);
        events.length = 0;
        const child = new Child();
        console.log('instance', events.join(','), child.value, child.parameter, child.computed, child.read(), child.automatic);
        console.log('presence', Object.hasOwn(child, 'value'), Object.hasOwn(child, 'parameter'), Object.hasOwn(child, 'untouched'));
        console.log('keys', Object.keys(child).join(','), child.observed);
        console.log('names', child.arrow.name, child.expression.name, child.klass.name, Child.callback.name);
        child.automatic = 14; console.log('accessor', child.automatic, new Child(6).automatic);
        class Plain {
          value = init('plain field', 1);
          constructor(public parameter: number = init('plain default', 2)) {events.push('plain body');}
        }
        events.length = 0;
        const plain = new Plain();
        console.log('plain', events.join(','), plain.value, plain.parameter);
        console.log('plain keys', Object.keys(plain).join(','));
        class Implicit extends (Parent as any) {value = 10;}
        console.log('implicit', new Implicit().value);
        class Uninitialized { [key]!: number; }
        console.log('uninitialized', Object.keys(new Uninitialized()).join(','));
        const replacement = {};
        class Replacer {constructor() {return replacement;}}
        class Replaced extends Replacer {
          value = 15;
          constructor(public parameter = 16) {super();}
        }
        const replaced = new Replaced();
        console.log('replacement', replaced === replacement, JSON.stringify(replaced));
        """;
    foreach (bool define in new[] {false, true}) {
        string directory = define ? "define fields" : "assign fields";
        Write(directory + "/main.ts", fields);
        Write(directory + "/tsconfig.json", "{\"compilerOptions\":{\"strict\":true,\"target\":\"es2022\",\"types\":[],\"useDefineForClassFields\":" + (define ? "true" : "false") + "},\"files\":[\"main.ts\"]}");
        CheckProject(directory);
    }
    Console.WriteLine("PASS class field options, inherited setters, constructor order, private fields and auto-accessors against Node");
    Write("decorated fields/tsconfig.json", """{"compilerOptions":{"strict":true,"target":"es2022","lib":["esnext","dom"],"types":[],"useDefineForClassFields":false},"files":["main.ts"]}""");
    Write("decorated fields/main.ts", """
        const events: string[] = [];
        function initialize(_value: undefined, context: ClassFieldDecoratorContext<any, any>) {
          context.addInitializer(function() {events.push('extra:' + this[context.name]);});
          return function(value: any) {events.push('init:' + value); return value ?? 17;};
        }
        class Decorated {
          @initialize value!: number;
          @initialize static value: number;
          constructor(public parameter = 4) {events.push('body:' + this.value);}
        }
        console.log('static', events.join(','), Decorated.value);
        events.length = 0;
        const item = new Decorated();
        console.log('instance', events.join(','), item.value, item.parameter, Object.keys(item).join(','));
        """);
    CheckProject("decorated fields");
    Console.WriteLine("PASS assignment-style decorator initializers without field initializers against Node");
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
