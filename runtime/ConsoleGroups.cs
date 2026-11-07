using System;
using System.IO;

public static partial class Js
{
    static int consoleGroupDepth;
    static void WriteConsole(string text, bool error = false)
    {
        if (consoleGroupDepth != 0) {
            string indent = new(' ', checked(consoleGroupDepth * 2));
            text = indent + text.Replace("\n", "\n" + indent, StringComparison.Ordinal);
        }
        if (error) Console.Error.WriteLine(text); else Console.WriteLine(text);
    }
    static object ConsoleGroup(object[] args)
    {
        if (args.Length != 0) WriteConsole(ConsoleMessage(args));
        consoleGroupDepth++;
        return Undefined;
    }
    static object ConsoleGroupEnd(object[] args)
    {
        if (consoleGroupDepth != 0) consoleGroupDepth--;
        return Undefined;
    }
    static object ConsoleClear(object[] args)
    {
        consoleGroupDepth = 0;
        if (!Console.IsOutputRedirected) {
            try { Console.Clear(); }
            catch (IOException) { }
        }
        return Undefined;
    }
}
