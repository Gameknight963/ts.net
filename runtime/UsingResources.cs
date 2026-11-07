using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class UsingResources
    {
        readonly List<(TsObject Value, JsFunction Dispose)> resources = new();
        internal bool HasError;
        internal object Error;
        internal object Add(object value)
        {
            if (IsNullish(value)) return value;
            if (value is not TsObject resource) throw new InvalidOperationException("Using resource must be an object");
            if (resource.Get(PropertyKey(DisposeSymbol)) is not JsFunction dispose) throw new InvalidOperationException("Using resource has no callable Symbol.dispose method");
            resources.Add((resource, dispose));
            return value;
        }
        internal object Dispose()
        {
            object pending = Error; bool failed = HasError;
            Error = null; HasError = false;
            while (resources.Count != 0) {
                int index = resources.Count - 1;
                var resource = resources[index]; resources.RemoveAt(index);
                try { resource.Dispose.InvokeWithReceiver(resource.Value, Array.Empty<object>()); }
                catch (Exception exception) {
                    object reason = CaughtValue(exception);
                    if (failed) {
                        var combined = new JsCaughtError("SuppressedError", "An error was suppressed during disposal.");
                        combined.DefineHidden("error", reason); combined.DefineHidden("suppressed", pending);
                        pending = combined;
                    } else { pending = reason; failed = true; }
                }
            }
            if (failed) throw MakeThrown(pending);
            return Undefined;
        }
    }
    static class UsingFunctions
    {
        internal static readonly JsFunction Stack = new((FunctionBody)(static _ => new UsingResources()), null, "", 0);
        internal static readonly JsFunction Add = new((FunctionBody)(static args => ((UsingResources)FunctionArgument(args, 0)).Add(FunctionArgument(args, 1))), null, "", 2);
        internal static readonly JsFunction Error = new((FunctionBody)(static args => {
            var stack = (UsingResources)FunctionArgument(args, 0); stack.Error = FunctionArgument(args, 1); stack.HasError = true; return Undefined;
        }), null, "", 2);
        internal static readonly JsFunction Dispose = new((FunctionBody)(static args => ((UsingResources)FunctionArgument(args, 0)).Dispose()), null, "", 1);
    }
}
