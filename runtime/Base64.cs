using System;
using System.Text;

public static partial class Js
{
    static class Base64Functions
    {
        internal static readonly JsFunction Encode = Create(true);
        internal static readonly JsFunction Decode = Create(false);
        static JsFunction Create(bool encode) => new((FunctionBody)(args =>
        {
            if (args.Length == 0) throw new InvalidOperationException("Base64 conversion requires an argument");
            string text = StringValue(args[0]);
            return encode ? EncodeBase64(text) : DecodeBase64(text);
        }), null, encode ? "btoa" : "atob", 1);
    }
    static Exception InvalidBase64() 
    {
        var error = new JsCaughtError("Error", "Invalid base64 input");
        error.DefineHidden("name", "InvalidCharacterError");
        return MakeThrown(error);
    }
    static string EncodeBase64(string text)
    {
        foreach (char value in text) if (value > 255) throw InvalidBase64();
        return Convert.ToBase64String(Encoding.Latin1.GetBytes(text));
    }
    static string DecodeBase64(string text)
    {
        var compact = new StringBuilder(text.Length);
        foreach (char value in text)
        {
            if (value is '\t' or '\n' or '\f' or '\r' or ' ') continue;
            if (!(value is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '+' or '/' or '=')) throw InvalidBase64();
            compact.Append(value);
        }
        if (compact.Length % 4 != 0 && !text.Contains('='))
        {
            if (compact.Length % 4 == 1) throw InvalidBase64();
            while (compact.Length % 4 != 0) compact.Append('=');
        }
        try { return Encoding.Latin1.GetString(Convert.FromBase64String(compact.ToString())); }
        catch (FormatException) { throw InvalidBase64(); }
    }
}
