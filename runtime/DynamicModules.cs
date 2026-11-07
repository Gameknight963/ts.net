using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class ModuleEvaluationGroup
    {
        internal readonly List<CompiledModule> Members = new();
        internal readonly JsPromise Completion = new() {Handled = true};
        internal int State; // 0 not started, 1 evaluating, 2 complete, 3 failed
        internal object Failure;
        int remaining;
        internal void Start(CompiledModule entry)
        {
            if (State != 0) return;
            State = 1; remaining = Members.Count;
            try {entry.EvaluateBody(new());}
            catch(Exception error) {Fail(CaughtValue(error));}
        }
        internal void CompleteMember()
        {
            if(State == 1 && --remaining == 0) {State = 2; Completion.Settle(Undefined, false);}
        }
        internal void Fail(object reason)
        {
            if(State is 2 or 3) return;
            State = 3; Failure = reason; Completion.Settle(reason, true);
        }
    }
    sealed class CompiledModule(TsObject exports, JsFunction initialize, CompiledModules registry)
    {
        internal bool IsJson;
        internal readonly TsObject Exports = exports;
        internal readonly List<CompiledModule> Dependencies = new();
        internal ModuleEvaluationGroup Group;
        int state;
        object failure;
        JsPromise bodyCompletion, evaluation;
        void Complete()
        {
            if(state is 2 or 3) return;
            state = 2; bodyCompletion.Settle(Exports, false); Group.CompleteMember();
        }
        void Fail(object reason)
        {
            if(state is 2 or 3) return;
            failure = reason; state = 3; bodyCompletion.Settle(reason, true); Group.Fail(reason);
        }
        void Start()
        {
            if(state == 3) return;
            if(Group.State == 3) {Fail(Group.Failure); return;}
            try {
                object result = initialize.InvokeWithReceiver(Undefined, Array.Empty<object>());
                if(result is JsPromise promise) promise.Then(new PromiseReaction(null,null,null,(value,rejected) => {if(rejected) Fail(value); else Complete();}));
                else Complete();
            } catch(Exception error) {Fail(CaughtValue(error));}
        }
        internal object EvaluateBody(HashSet<CompiledModule> active)
        {
            if(state == 3) throw MakeThrown(failure);
            if(state == 2) return Exports;
            if(state == 1) return active.Contains(this) ? Exports : bodyCompletion;
            state = 1; bodyCompletion = new JsPromise {Handled = true}; active.Add(this);
            try {
                var waiting = new List<JsPromise>();
                foreach(var dependency in Dependencies) {
                    object result = ReferenceEquals(Group, dependency.Group) ? dependency.EvaluateBody(active) : dependency.Evaluate();
                    if(result is JsPromise promise) waiting.Add(promise);
                }
                int remaining = waiting.Count;
                if(remaining == 0) Start();
                else foreach(var promise in waiting) promise.Then(new PromiseReaction(null,null,null,(value,rejected) => {
                    if(rejected) Fail(value); else if(--remaining == 0) Start();
                }));
            } catch(Exception error) {Fail(CaughtValue(error));}
            finally {active.Remove(this);}
            if(state == 3) throw MakeThrown(failure);
            return state == 2 ? Exports : bodyCompletion;
        }
        internal object Evaluate()
        {
            registry.EnsureGroups(); Group.Start(this);
            if(Group.State == 3) throw MakeThrown(Group.Failure);
            if(Group.State == 2) return Exports;
            if(evaluation == null) {
                evaluation = new JsPromise {Handled = true};
                Group.Completion.Then(new PromiseReaction(null,null,null,(value,rejected) => evaluation.Settle(rejected ? value : Exports,rejected)));
            }
            return evaluation;
        }
    }
    sealed class CompiledModules
    {
        internal readonly Dictionary<string, CompiledModule> Files = new(StringComparer.Ordinal);
        internal readonly Dictionary<(string, string), CompiledModule> Aliases = new();
        bool grouped;
        // Imports wait for the entire cycle, even if one member finishes early.
        internal void EnsureGroups()
        {
            if(grouped) return;
            var indices = new Dictionary<CompiledModule,int>();
            var low = new Dictionary<CompiledModule,int>();
            var pending = new Stack<CompiledModule>();
            var active = new HashSet<CompiledModule>();
            int index = 0;
            void Visit(CompiledModule module)
            {
                indices[module] = low[module] = index++; pending.Push(module); active.Add(module);
                foreach(var dependency in module.Dependencies) {
                    if(!indices.ContainsKey(dependency)) {Visit(dependency); low[module] = Math.Min(low[module],low[dependency]);}
                    else if(active.Contains(dependency)) low[module] = Math.Min(low[module],indices[dependency]);
                }
                if(low[module] != indices[module]) return;
                var group = new ModuleEvaluationGroup();
                CompiledModule member;
                do {member = pending.Pop(); active.Remove(member); member.Group = group; group.Members.Add(member);} while(!ReferenceEquals(member,module));
            }
            foreach(var module in Files.Values) if(!indices.ContainsKey(module)) Visit(module);
            grouped = true;
        }

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
    [ThreadStatic] static List<CompiledModule> moduleRoots;
    static void CheckModuleEvaluations()
    {
        if(moduleRoots == null) return;
        foreach(var module in moduleRoots) {
            if(module.Group.State == 3) throw MakeThrown(module.Group.Failure);
            if(module.Group.State != 2) throw MakeThrown(new JsCaughtError("Error", "Module initialization did not complete"));
        }
    }
    static void ClearModuleEvaluations() => moduleRoots?.Clear();
    static object ImportType(object options)
    {
        if (IsUndefined(options)) return Undefined;
        if (options is not TsObject obj) throw new InvalidOperationException("Import options must be an object");
        object value = obj.Get("with");
        if (IsUndefined(value)) return Undefined;
        if (value is not TsObject attributes) throw new InvalidOperationException("Import attributes must be an object");
        var keys = (JsArray)attributes.OwnKeys(); object type = Undefined;
        for (uint index = 0; index < keys.Length(); index++)
        {
            string key = (string)keys.ElementAt(index);
            if (IsSymbolKey(key)) continue;
            object setting = attributes.Get(key);
            if (setting is not string || key != "type") throw new InvalidOperationException("Unsupported import attribute");
            type = setting;
        }
        return type;
    }
    static class DynamicModuleFunctions
    {
        internal static readonly JsFunction Registry = new((FunctionBody)(static _ => new CompiledModules()), null, "", 0);
        internal static readonly JsFunction Create = new((FunctionBody)(static args => {
            var registry = (CompiledModules)FunctionArgument(args, 0);
            var exports = FunctionArgument(args, 2) as TsObject;
            if (exports == null) {exports = new JsDynamicObject(); SealModuleNamespace(exports);}
            var module = new CompiledModule(exports, (JsFunction)FunctionArgument(args, 3), registry);
            var url = new Uri(StringValue(FunctionArgument(args, 1)), UriKind.Absolute);
            module.IsJson = url.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
            registry.Files.Add(url.AbsoluteUri, module); return module;
        }), null, "", 4);
        internal static readonly JsFunction Depend = new((FunctionBody)(static args => {((CompiledModule)FunctionArgument(args, 0)).Dependencies.Add((CompiledModule)FunctionArgument(args, 1));return Undefined;}), null, "", 2);
        internal static readonly JsFunction Alias = new((FunctionBody)(static args => {
            ((CompiledModules)FunctionArgument(args, 0)).Aliases[(StringValue(FunctionArgument(args, 1)), StringValue(FunctionArgument(args, 2)))] = (CompiledModule)FunctionArgument(args, 3);return Undefined;
        }), null, "", 4);
        internal static readonly JsFunction Run = new((FunctionBody)(static args => {
            var module = (CompiledModule)FunctionArgument(args, 0);
            object result = module.Evaluate();
            if(result is JsPromise promise) {
                (moduleRoots ??= new()).Add(module);
                var observed = new JsPromise();
                promise.Then(new PromiseReaction(null,null,null,(value,rejected) => observed.Settle(value,rejected)));
                return observed;
            }
            return result;
        }), null, "", 1);
        internal static readonly JsFunction Import = new((FunctionBody)(static args => {
            var promise = new JsPromise();
            var registry = (CompiledModules)FunctionArgument(args, 0);
            string issuer = StringValue(FunctionArgument(args, 1)), specifier;
            object type;
            try {specifier = StringValue(FunctionArgument(args, 2)); type = ImportType(FunctionArgument(args, 3));}
            catch (Exception error) {promise.Settle(CaughtValue(error), true);return promise;}
            EnqueuePromiseJob(() => {
                try {
                    var module = registry.Resolve(issuer, specifier);
                    if (!IsUndefined(type) && (!module.IsJson || (string)type != "json")) throw new InvalidOperationException("Import type does not match the compiled module");
                    promise.ResolveValue(module.Evaluate());
                }
                catch (Exception error) {promise.Settle(CaughtValue(error), true);}
            });
            return promise;
        }), null, "", 3);
    }
}
