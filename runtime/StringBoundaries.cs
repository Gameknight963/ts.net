using System;
using System.Text;

public static partial class Js
{
    sealed class StringBoundaryFunction(string name, bool backwards) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, name, 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            string search = StringValue(FunctionArgument(arguments, 0));
            object position = FunctionArgument(arguments, 1);
            int bound = backwards && IsUndefined(position) ? text.Length : StringRangeBound(Number(position), text.Length, true);
            int start = backwards ? bound - search.Length : bound;
            return BoxBoolean(start >= 0 && search.Length <= text.Length - start && text.AsSpan(start, search.Length).SequenceEqual(search));
        }
    }
    sealed class StringConcatFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, "concat", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            if (arguments.Length == 0) return text;
            if (arguments.Length == 1) return string.Concat(text, StringValue(arguments[0]));
            var result = new StringBuilder(text);
            foreach (object argument in arguments) result.Append(StringValue(argument));
            return result.ToString();
        }
    }
    static void DefineStringBoundaries(JsBoxedPrimitive prototype)
    {
        prototype.DefineHidden("split", new StringSplitFunction());
        prototype.DefineHidden("startsWith", new StringBoundaryFunction("startsWith", false));
        prototype.DefineHidden("endsWith", new StringBoundaryFunction("endsWith", true));
        prototype.DefineHidden("concat", new StringConcatFunction());
    }
}
