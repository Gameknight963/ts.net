using System;

public static partial class Js
{
    static class RegExpMethods
    {
        internal static readonly JsFunction Exec = new CollectionMethod("exec", 1, static (receiver, args) => receiver is JsRegExp regex
            ? RegExpMatch(regex, FunctionArgument(args, 0), false)
            : throw new InvalidOperationException("RegExp.exec requires a regular expression"));
    }
    static object RegExpTest(object receiver, object[] args)
    {
        if (receiver is not TsObject source) throw new InvalidOperationException("RegExp.test requires an object");
        string text = StringValue(FunctionArgument(args, 0));
        object exec = source.Get("exec");
        if (exec is JsFunction method && !ReferenceEquals(method, RegExpMethods.Exec))
        {
            object result = method.InvokeWithReceiver(receiver, new object[] { text });
            if (result == null) return BoxBoolean(false);
            if (result is not TsObject) throw new InvalidOperationException("RegExp.exec must return an object or null");
            return BoxBoolean(true);
        }
        if (receiver is not JsRegExp regex) throw new InvalidOperationException("RegExp.exec requires a regular expression");
        return RegExpMatch(regex, text, true);
    }
}
