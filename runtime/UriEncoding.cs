using System;
using System.Text;

public static partial class Js
{
    const string UriReserved = ";,/?:@&=+$#";
    static class UriFunctions
    {
        internal static readonly JsFunction Encode = Create("encodeURI", true, false), EncodeComponent = Create("encodeURIComponent", true, true);
        internal static readonly JsFunction Decode = Create("decodeURI", false, false), DecodeComponent = Create("decodeURIComponent", false, true);
        static JsFunction Create(string name, bool encode, bool component) => new((FunctionBody)(args => encode ? EncodeUri(StringValue(FunctionArgument(args, 0)), component) : DecodeUri(StringValue(FunctionArgument(args, 0)), component)), null, name, 1);
    }
    static Exception MalformedUri() => MakeThrown(new JsCaughtError("URIError", "URI malformed"));
    static bool UriUnescaped(byte value) => value is >= (byte)'a' and <= (byte)'z' or >= (byte)'A' and <= (byte)'Z' or >= (byte)'0' and <= (byte)'9' || "-_.!~*'()".Contains((char)value);
    static string EncodeUri(string text, bool component)
    {
        if (FirstUnpairedSurrogate(text) >= 0) throw MalformedUri();
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        var result = new StringBuilder(text.Length);
        const string hex = "0123456789ABCDEF";
        foreach (byte value in bytes)
        {
            if (UriUnescaped(value) || !component && UriReserved.Contains((char)value)) result.Append((char)value);
            else { result.Append('%'); result.Append(hex[value >> 4]); result.Append(hex[value & 15]); }
        }
        return result.ToString();
    }
    static int UriHex(char value) => value is >= '0' and <= '9' ? value - '0' : value is >= 'a' and <= 'f' ? value - 'a' + 10 : value is >= 'A' and <= 'F' ? value - 'A' + 10 : -1;
    static int UriByte(string text, int index)
    {
        if (text.Length - index < 3 || text[index] != '%') throw MalformedUri();
        int high = UriHex(text[index + 1]), low = UriHex(text[index + 2]);
        if (high < 0 || low < 0) throw MalformedUri();
        return (high << 4) | low;
    }
    static string DecodeUri(string text, bool component)
    {
        var result = new StringBuilder(text.Length);
        for (int index = 0; index < text.Length;)
        {
            if (text[index] != '%') { result.Append(text[index++]); continue; }
            int start = index, first = UriByte(text, index); index += 3;
            if (first < 0x80)
            {
                if (!component && UriReserved.Contains((char)first)) result.Append(text, start, 3);
                else result.Append((char)first);
                continue;
            }
            int count = first is >= 0xc2 and <= 0xdf ? 2 : first is >= 0xe0 and <= 0xef ? 3 : first is >= 0xf0 and <= 0xf4 ? 4 : 0;
            if (count == 0) throw MalformedUri();
            int point = first & ((1 << (7 - count)) - 1);
            for (int part = 1; part < count; part++)
            {
                int next = UriByte(text, index); index += 3;
                if ((next & 0xc0) != 0x80) throw MalformedUri();
                point = (point << 6) | (next & 0x3f);
            }
            int minimum = count == 2 ? 0x80 : count == 3 ? 0x800 : 0x10000;
            if (point < minimum || point > 0x10ffff || point is >= 0xd800 and <= 0xdfff) throw MalformedUri();
            if (point <= 0xffff) result.Append((char)point);
            else { point -= 0x10000; result.Append((char)(0xd800 + (point >> 10))); result.Append((char)(0xdc00 + (point & 0x3ff))); }
        }
        return result.ToString();
    }
}
