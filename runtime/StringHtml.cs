using System;

public static partial class Js
{
    sealed class StringHtmlFunction(string name, string tag, string attribute = null) : JsFunction(
        (FunctionBody)(static _ => throw new InvalidOperationException("String method requires a receiver")), null, name, attribute is null ? 0 : 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            string text = AtString(receiver);
            if (attribute is null) return $"<{tag}>{text}</{tag}>";
            string value = StringValue(FunctionArgument(arguments, 0)).Replace("\"", "&quot;", StringComparison.Ordinal);
            return $"<{tag} {attribute}=\"{value}\">{text}</{tag}>";
        }
    }
    static void DefineStringHtml(JsBoxedPrimitive prototype)
    {
        prototype.DefineHidden("anchor", new StringHtmlFunction("anchor", "a", "name"));
        prototype.DefineHidden("big", new StringHtmlFunction("big", "big"));
        prototype.DefineHidden("blink", new StringHtmlFunction("blink", "blink"));
        prototype.DefineHidden("bold", new StringHtmlFunction("bold", "b"));
        prototype.DefineHidden("fixed", new StringHtmlFunction("fixed", "tt"));
        prototype.DefineHidden("fontcolor", new StringHtmlFunction("fontcolor", "font", "color"));
        prototype.DefineHidden("fontsize", new StringHtmlFunction("fontsize", "font", "size"));
        prototype.DefineHidden("italics", new StringHtmlFunction("italics", "i"));
        prototype.DefineHidden("link", new StringHtmlFunction("link", "a", "href"));
        prototype.DefineHidden("small", new StringHtmlFunction("small", "small"));
        prototype.DefineHidden("strike", new StringHtmlFunction("strike", "strike"));
        prototype.DefineHidden("sub", new StringHtmlFunction("sub", "sub"));
        prototype.DefineHidden("sup", new StringHtmlFunction("sup", "sup"));
    }
}
