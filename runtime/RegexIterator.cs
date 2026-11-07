using System;

internal sealed class JsRegexIterator(JsRegExp regex, string text) : TsObject
{
    internal bool Done;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.RegexIteratorPrototype;
    internal object Next()
    {
        if (Done) return Js.IteratorResult(Js.Undefined, true);
        object value = Js.RegExpMatch(regex, text, false);
        if (value == null) { Done = true; regex = null; text = null; return Js.IteratorResult(Js.Undefined, true); }
        if (((string)Js.Get(value, "0")).Length == 0) regex.LastIndex = Js.Number(regex.LastIndex) + 1;
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
            return prototype;
        }
    }
    internal static TsObject RegexIteratorPrototype => RegexIteratorMethods.Prototype;
    internal static object StartRegexIterator(JsRegexIterator iterator)
    {
        if (iterator.Get("next") is not JsFunction next) throw new InvalidOperationException("Iterator next is not callable");
        return new JsProtocolIterator(iterator, next);
    }
    sealed class StringMatchAllFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "matchAll", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            object pattern = FunctionArgument(arguments, 0);
            JsRegExp copy;
            if (pattern is JsRegExp regex)
            {
                if (!regex.Flags.Contains('g')) throw new InvalidOperationException("matchAll requires a global regular expression");
                copy = new JsRegExp(regex.Pattern, regex.Flags, regex.Engine);
                double position = Number(regex.LastIndex);
                copy.LastIndex = double.IsNaN(position) || position <= 0 ? 0d : Math.Min(Math.Truncate(position), 9007199254740991d);
            }
            else copy = CreateRegExp(pattern, "g");
            return new JsRegexIterator(copy, text);
        }
    }
}
