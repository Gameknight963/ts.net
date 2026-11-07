using System.Diagnostics;

public static partial class Js
{
    [DebuggerHidden]
    public static void DebugStatement()
    {
        if (Debugger.IsAttached) Debugger.Break();
    }
}
