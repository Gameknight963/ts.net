using System.Text;
namespace Tsnet.Compiler;

internal static class NativeFrontend
{
    internal static byte[] Read(string path, bool experimentalDecorators = false)
    {
        string executable = Path.Combine(AppContext.BaseDirectory, OperatingSystem.IsWindows() ? "tsnet-frontend.exe" : "tsnet-frontend");
        var start = new System.Diagnostics.ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (experimentalDecorators) start.ArgumentList.Add("--experimentalDecorators");
        start.ArgumentList.Add(path);
        using var process = System.Diagnostics.Process.Start(start) ?? throw new CompilationException("Cannot start TypeScript frontend");
        var errors = process.StandardError.ReadToEndAsync();
        using var data = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(data);
        process.WaitForExit();
        string report = errors.GetAwaiter().GetResult();
        if (process.ExitCode != 0) throw new CompilationException(report.Trim());
        return data.ToArray();
    }
}
internal static class Program
{
    static int Main(string[] args)
    {
        try
        {
            bool experimentalDecorators = args.Contains("--experimentalDecorators");
            args = args.Where(arg => arg != "--experimentalDecorators").ToArray();
            if (args.Length != 2) { Console.Error.WriteLine("Usage: tsnetc [--experimentalDecorators] INPUT.ts OUTPUT.dll"); return 2; }
            var compiler = Compiler.create(args[0]);
            CompilerFunctions.import_frontend(args[0], compiler, experimentalDecorators);
            CompilerFunctions.emit_assembly(args[1], compiler);
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("tsnetc: " + error.Message); return 1; }
    }
}
