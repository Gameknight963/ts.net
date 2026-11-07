using System;

internal sealed class JsRawJson(string text) : TsObject
{
    internal readonly string Text = text;
    internal override TsObject PropertyPrototype => null;
    internal override bool TrySetPrototype(TsObject value) => value == null;
    internal override void SetPrototype(TsObject value) { if (value != null) throw new InvalidOperationException("Raw JSON object is not extensible"); }
    internal override bool IsReadOnlyOwnProperty(string key) => key == "rawJSON";
    public override bool HasOwn(string key) => key == "rawJSON";
    public override object Get(string key) => key == "rawJSON" ? Text : Js.Undefined;
    public override object Set(string key, object value) => throw new InvalidOperationException("Raw JSON object is frozen");
    public override object SetOwnValue(string key, object value) => throw new InvalidOperationException("Raw JSON object is frozen");
    public override object OwnKeys() => new JsArray(new object[] { "rawJSON" });
    public override double OwnKeyCount() => 1;
    public override object CloneForSpread() { var result = new JsDynamicObject(); result.DefineOwn("rawJSON", Text); return result; }
}

public static partial class Js
{
    static object CreateRawJson(object input)
    {
        string text = StringValue(input);
        if (text.Length == 0 || text[0] is ' ' or '\t' or '\n' or '\r' || text[^1] is ' ' or '\t' or '\n' or '\r')
            throw MakeThrown(new JsCaughtError("SyntaxError", "Raw JSON must not have surrounding whitespace"));
        var parsed = new JsonParser(text, false).Parse();
        if (parsed.Value is TsObject) throw MakeThrown(new JsCaughtError("SyntaxError", "Raw JSON must contain a primitive"));
        return new JsRawJson(text);
    }
}
