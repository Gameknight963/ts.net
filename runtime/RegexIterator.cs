using System;

internal sealed class JsRegexIterator(TsObject regex, string text, bool global, bool unicode) : TsObject
{
    internal bool Done;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.RegexIteratorPrototype;
    internal object Next()
    {
        if (Done) return Js.IteratorResult(Js.Undefined, true);
        object value = Js.ExecuteRegExp(regex, text);
        if (value == null) { Done = true; regex = null; text = null; return Js.IteratorResult(Js.Undefined, true); }
        if (!global) Done = true;
        else if (Js.StringValue(Js.Get(value, "0")).Length == 0)
        {
            double index = Js.Number(regex.Get("lastIndex"));
            index = double.IsNaN(index) || index <= 0 ? 0 : Math.Min(9007199254740991d, Math.Floor(index));
            int step = unicode && index < text.Length - 1 && char.IsHighSurrogate(text[(int)index]) && char.IsLowSurrogate(text[(int)index + 1]) ? 2 : 1;
            regex.Set("lastIndex", index + step);
        }
        return Js.IteratorResult(value, false);
    }
}

public static partial class Js
{
    static class RegexIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var prototype = new JsClassInstance { ParentPrototype = NativeIteratorPrototype };
            prototype.DefineMethod("next", new CollectionMethod("next", 0, (receiver, _) => receiver is JsRegexIterator iterator ? iterator.Next() : throw new InvalidOperationException("Incompatible iterator receiver")));
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "RegExp String Iterator");
            return prototype;
        }
    }
    internal static TsObject RegexIteratorPrototype => RegexIteratorMethods.Prototype;
    internal static object StartRegexIterator(JsRegexIterator iterator)
    {
        return new JsProtocolIterator(iterator, iterator.Get("next"));
    }
    sealed class StringMatchAllFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "matchAll", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            object pattern = FunctionArgument(arguments, 0);
            if (IsNullish(receiver)) throw new InvalidOperationException("String method requires a receiver");
            RequireGlobalRegExp(pattern, "matchAll");
            if (TryStringProtocol(receiver, pattern, MatchAllSymbol, Undefined, false, out var custom)) return custom;
            string text = AtString(receiver);
            return MatchAllRegExp(text, pattern as JsRegExp ?? CreateRegExp(pattern, "g"));
        }
    }
    static object MatchAllRegExp(string text, TsObject regex)
    {
        JsFunction species = null;
        object constructor = regex.Get("constructor");
        if (!IsUndefined(constructor))
        {
            if (constructor is not TsObject obj) throw new InvalidOperationException("RegExp constructor must be an object");
            object requested = obj.Get(PropertyKey(SpeciesSymbol));
            if (!IsNullish(requested))
                species = requested is JsFunction function && function.CanInitializeDerived ? function : throw new InvalidOperationException("RegExp species must be a constructor");
        }
        string flags = StringValue(regex.Get("flags"));
        TsObject copy;
        if ((species == null || ReferenceEquals(species, RuntimeNamespaces.RegExp)) && regex is JsRegExp native && flags == native.Flags)
            copy = new JsRegExp(native.Pattern, native.Flags, native.Engine, native.NamedGroups);
        else copy = species == null ? CreateRegExp(regex, flags) : species.Construct(new object[] { regex, flags }) as TsObject ?? throw new InvalidOperationException("RegExp species returned a primitive");
        double position = Number(regex.Get("lastIndex"));
        copy.Set("lastIndex", double.IsNaN(position) || position <= 0 ? 0d : Math.Min(Math.Truncate(position), 9007199254740991d));
        return new JsRegexIterator(copy, text, flags.Contains('g'), flags.Contains('u') || flags.Contains('v'));
    }

}
