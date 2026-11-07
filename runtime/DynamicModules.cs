using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class CompiledModule(TsObject exports, JsFunction initialize)
    {
        internal readonly TsObject Exports = exports;
        internal readonly List<CompiledModule> Dependencies = new();
        int state; // 0 uninitialized, 1 evaluating, 2 complete, 3 failed
        object failure;
        internal object Evaluate()
        {
            if (state == 3) throw MakeThrown(failure);
            if (state != 0) return Exports;
            state = 1;
            try {
                foreach (var dependency in Dependencies) dependency.Evaluate();
                initialize.InvokeWithReceiver(Undefined, Array.Empty<object>());
                state = 2; return Exports;
            } catch (Exception error) {failure = CaughtValue(error); state = 3; throw MakeThrown(failure);}
        }
    }
    sealed class CompiledModules
    {
        internal readonly Dictionary<string, CompiledModule> Files = new(StringComparer.Ordinal);
        internal readonly Dictionary<(string, string), CompiledModule> Aliases = new();
        internal CompiledModule Resolve(string issuer, string specifier)
        {
            if (Aliases.TryGetValue((issuer, specifier), out var known)) return known;
            if (!Uri.TryCreate(new Uri(issuer, UriKind.Absolute), specifier, out var requested))
                throw MakeThrown(new JsCaughtError("Error", "Invalid module specifier"));
            string path = requested.AbsoluteUri;
            if (Files.TryGetValue(path, out known)) return known;
            if (path.EndsWith(".js", StringComparison.Ordinal) && Files.TryGetValue(path[..^3] + ".ts", out known)) return known;
            throw MakeThrown(new JsCaughtError("Error", "Module is not included in this assembly: " + specifier));
        }
    }
    static class DynamicModuleFunctions
    {
        internal static readonly JsFunction Registry = new((FunctionBody)(static _ => new CompiledModules()), null, "", 0);
        internal static readonly JsFunction Create = new((FunctionBody)(static args => {
            var registry = (CompiledModules)FunctionArgument(args, 0);
            var exports = FunctionArgument(args, 2) as TsObject;
            if (exports == null) {exports = new JsDynamicObject(); SealModuleNamespace(exports);}
            var module = new CompiledModule(exports, (JsFunction)FunctionArgument(args, 3));
            registry.Files.Add(new Uri(StringValue(FunctionArgument(args, 1)), UriKind.Absolute).AbsoluteUri, module); return module;
        }), null, "", 4);
        internal static readonly JsFunction Depend = new((FunctionBody)(static args => {((CompiledModule)FunctionArgument(args, 0)).Dependencies.Add((CompiledModule)FunctionArgument(args, 1));return Undefined;}), null, "", 2);
        internal static readonly JsFunction Alias = new((FunctionBody)(static args => {
            ((CompiledModules)FunctionArgument(args, 0)).Aliases[(StringValue(FunctionArgument(args, 1)), StringValue(FunctionArgument(args, 2)))] = (CompiledModule)FunctionArgument(args, 3);return Undefined;
        }), null, "", 4);
        internal static readonly JsFunction Run = new((FunctionBody)(static args => ((CompiledModule)FunctionArgument(args, 0)).Evaluate()), null, "", 1);
        internal static readonly JsFunction Import = new((FunctionBody)(static args => {
            var promise = new JsPromise();
            var registry = (CompiledModules)FunctionArgument(args, 0);
            string issuer = StringValue(FunctionArgument(args, 1)), specifier;
            try {specifier = StringValue(FunctionArgument(args, 2));}
            catch (Exception error) {promise.Settle(CaughtValue(error), true);return promise;}
            EnqueuePromiseJob(() => {
                try {promise.ResolveValue(registry.Resolve(issuer, specifier).Evaluate());}
                catch (Exception error) {promise.Settle(CaughtValue(error), true);}
            });
            return promise;
        }), null, "", 3);
    }
}
