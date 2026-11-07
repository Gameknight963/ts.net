using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class DecoratorMember
    {
        internal JsArray Decorators;
        internal string Kind, Key;
        internal bool Static;
        internal JsPrivateMember Private;
        internal readonly List<JsFunction> Initializers = new(), Extras = new();
    }
    sealed class DecoratorPlan(JsArray decorators)
    {
        internal readonly JsArray ClassDecorators = decorators;
        internal readonly List<DecoratorMember> Members = new();
        readonly List<JsFunction> instanceExtras = new(), staticExtras = new(), classExtras = new();
        object metadata = Undefined;
        JsClassFunction home;
        internal DecoratorMember Member(int index)
        {
            while (Members.Count <= index) Members.Add(new DecoratorMember());
            return Members[index];
        }
        static object Run(List<JsFunction> functions, object receiver)
        {
            foreach (var function in functions) function.InvokeWithReceiver(receiver, Array.Empty<object>());
            return Undefined;
        }
        internal object RunMethods(object receiver, bool isStatic) => Run(isStatic ? staticExtras : instanceExtras, receiver);
        internal object RunClass(object receiver) => Run(classExtras, receiver);
        static JsFunction Callable(object value) => value as JsFunction ?? throw new InvalidOperationException("Decorator result must be a function");
        static object Name(string key) => symbolKeys.TryGetValue(key, out var symbol) ? symbol : key;
        object Context(DecoratorMember member, string kind, string key, List<JsFunction> extras, out Action finish)
        {
            var context = new JsDynamicObject();
            context.DefineOwn("kind", kind); context.DefineOwn("name", Name(key)); context.DefineOwn("metadata", metadata);
            bool active = true;
            context.DefineOwn("addInitializer", new JsFunction((FunctionBody)(args =>
            {
                if (!active) throw new InvalidOperationException("Decorator initializers must be registered during decoration");
                extras.Add(Callable(FunctionArgument(args, 0))); return Undefined;
            }), null, "addInitializer", 1));
            finish = () => active = false;
            if (member == null) return context;
            context.DefineOwn("static", BoxBoolean(member.Static)); context.DefineOwn("private", BoxBoolean(member.Private != null));
            var access = new JsDynamicObject();
            access.DefineOwn("has", new JsFunction((FunctionBody)(args => BoxBoolean(member.Private != null ? member.Private.Has(FunctionArgument(args, 0)) : FunctionArgument(args, 0) is TsObject obj ? obj.HasInProperty(key) : throw new InvalidOperationException("Decorator access requires an object"))), null, "has", 1));
            if (kind != "setter") access.DefineOwn("get", new JsFunction((FunctionBody)(args => member.Private != null ? member.Private.Read(FunctionArgument(args, 0)) : Get(FunctionArgument(args, 0), key)), null, "get", 1));
            if (kind is "field" or "accessor" or "setter") access.DefineOwn("set", new JsFunction((FunctionBody)(args =>
            {
                object receiver = FunctionArgument(args, 0), value = FunctionArgument(args, 1);
                if (member.Private != null) member.Private.Write(receiver, value); else Set(receiver, key, value);
                return Undefined;
            }), null, "set", 2));
            context.DefineOwn("access", access);
            return context;
        }
        static object AccessorValue(object get, object set)
        {
            var result = new JsDynamicObject(); result.DefineOwn("get", get ?? Undefined); result.DefineOwn("set", set ?? Undefined); return result;
        }
        void ApplyMember(DecoratorMember member)
        {
            TsObject target = member.Static ? home : home.Prototype;
            object value = Undefined, getter = null, setter = null;
            if (member.Kind == "accessor")
            {
                if (member.Private != null) { getter = member.Private.DecoratorFunction("getter"); setter = member.Private.DecoratorFunction("setter"); }
                else if (target.TryOwnAccessor(member.Key, out var accessor)) { getter = accessor.Getter; setter = accessor.Setter; }
                value = AccessorValue(getter, setter);
            }
            else if (member.Kind != "field")
            {
                if (member.Private != null) value = member.Private.DecoratorFunction(member.Kind);
                else if (member.Kind == "method") value = target.Get(member.Key);
                else if (target.TryOwnAccessor(member.Key, out var accessor)) value = member.Kind == "getter" ? accessor.Getter : accessor.Setter;
            }
            var extras = member.Kind is "field" or "accessor" ? member.Extras : member.Static ? staticExtras : instanceExtras;
            for (long i = (long)member.Decorators.Length() - 1; i >= 0; i--)
            {
                object context = Context(member, member.Kind, member.Key, extras, out var finish);
                object result;
                try { result = Callable(member.Decorators.ElementAt((uint)i)).InvokeWithReceiver(Undefined, new[] { value, context }); }
                finally { finish(); }
                if (IsUndefined(result)) continue;
                if (member.Kind == "field") member.Initializers.Insert(0, Callable(result));
                else if (member.Kind == "accessor")
                {
                    if (result is not TsObject obj) throw new InvalidOperationException("Accessor decorator must return an object");
                    object get = obj.Get("get"), set = obj.Get("set"), init = obj.Get("init");
                    if (!IsUndefined(get)) getter = Callable(get);
                    if (!IsUndefined(set)) setter = Callable(set);
                    if (!IsUndefined(init)) member.Initializers.Insert(0, Callable(init));
                    value = AccessorValue(getter, setter);
                }
                else value = Callable(result);
            }
            if (member.Kind == "field") return;
            if (member.Kind == "accessor")
            {
                SetFunction(member, "getter", getter); SetFunction(member, "setter", setter);
            }
            else SetFunction(member, member.Kind, value);
        }
        void SetFunction(DecoratorMember member, string kind, object value)
        {
            if (value is not JsFunction function) return;
            string name = function.DisplayName;
            if (member.Private != null) member.Private.Decorate(kind, function);
            else if (kind == "method") home.DefineMethod(member.Key, function, member.Static);
            else DefineClassMethod(home, member.Key, function, member.Static, kind == "getter" ? 1 : 2);
            // Accessor installation normally infers a name; keep the returned name.
            function.InferPropertyName(name);
        }
        internal object Apply(JsClassFunction target)
        {
            home = target;
            object metadataKey = RuntimeNamespaces.Symbol.Get("metadata");
            if (metadataKey is JsSymbol)
            {
                var data = new JsDynamicObject();
                object inherited = home.Parent?.Get(PropertyKey(metadataKey));
                data.SetPrototype(inherited as TsObject); metadata = data;
            }
            // Accessors decorate with methods; fields decorate after both method groups.
            for (int group = 0; group < 4; group++)
                foreach (var member in Members)
                    if ((member.Kind == "field" ? 2 : 0) + (member.Static ? 0 : 1) == group) ApplyMember(member);
            object result = home;
            string name = (string)home.Get("name");
            for (long i = (long)ClassDecorators.Length() - 1; i >= 0; i--)
            {
                object context = Context(null, "class", name, classExtras, out var finish);
                object replacement;
                try { replacement = Callable(ClassDecorators.ElementAt((uint)i)).InvokeWithReceiver(Undefined, new[] { result, context }); }
                finally { finish(); }
                if (!IsUndefined(replacement))
                {
                    if (replacement is not JsFunction function || !function.CanInitializeDerived) throw new InvalidOperationException("Class decorator must return a constructor");
                    result = replacement;
                }
            }
            if (metadataKey is JsSymbol) ((TsObject)result).DefineOwn(PropertyKey(metadataKey), metadata);
            return result;
        }
    }
    static class DecoratorFunctions
    {
        static DecoratorPlan Plan(object[] args) => (DecoratorPlan)FunctionArgument(args, 0);
        static int Index(object[] args) => (int)Number(FunctionArgument(args, 1));
        internal static readonly JsFunction Create = new((FunctionBody)(static args => new DecoratorPlan((JsArray)FunctionArgument(args, 0))), null, "", 1);
        internal static readonly JsFunction Store = new((FunctionBody)(static args => { Plan(args).Member(Index(args)).Decorators = (JsArray)FunctionArgument(args, 2); return Undefined; }), null, "", 3);
        internal static readonly JsFunction Configure = new((FunctionBody)(static args =>
        {
            var member = Plan(args).Member(Index(args)); member.Kind = (string)FunctionArgument(args, 2); member.Key = PropertyKey(FunctionArgument(args, 3)); member.Static = Truthy(FunctionArgument(args, 4)); member.Private = FunctionArgument(args, 5) as JsPrivateMember;
            return Undefined;
        }), null, "", 6);
        internal static readonly JsFunction Apply = new((FunctionBody)(static args => Plan(args).Apply((JsClassFunction)FunctionArgument(args, 1))), null, "", 2);
        internal static readonly JsFunction Run = new((FunctionBody)(static args => Plan(args).RunMethods(FunctionArgument(args, 1), Truthy(FunctionArgument(args, 2)))), null, "", 3);
        internal static readonly JsFunction Init = new((FunctionBody)(static args =>
        {
            object receiver = FunctionArgument(args, 2), value = FunctionArgument(args, 3);
            foreach (var function in Plan(args).Member(Index(args)).Initializers) value = function.InvokeWithReceiver(receiver, new[] { value });
            return value;
        }), null, "", 4);
        internal static readonly JsFunction After = new((FunctionBody)(static args => { foreach (var function in Plan(args).Member(Index(args)).Extras) function.InvokeWithReceiver(FunctionArgument(args, 2), Array.Empty<object>()); return Undefined; }), null, "", 3);
        internal static readonly JsFunction Done = new((FunctionBody)(static args => Plan(args).RunClass(FunctionArgument(args, 1))), null, "", 2);
    }
}
