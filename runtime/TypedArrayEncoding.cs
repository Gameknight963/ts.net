using System;

public static partial class Js
{
    static Exception InvalidByteText() => MakeThrown(new JsCaughtError("SyntaxError", "Invalid byte encoding"));
    static TypedArrayValue RequireByteArray(object receiver) => receiver is TypedArrayValue { Name: "Uint8Array" } array ? array : throw new InvalidOperationException("Uint8Array receiver required");
    static Span<byte> ByteArrayBytes(TypedArrayValue array)
    {
        if (array.OutOfBounds) throw new InvalidOperationException("Uint8Array is out of bounds");
        return array.Buffer.Bytes.AsSpan(array.Offset, array.Count);
    }
    static string ByteText(object value) => value as string ?? throw new InvalidOperationException("Byte encoding input must be a string");
    static TsObject ByteEncodingOptions(object value) => IsUndefined(value) ? null : value as TsObject ?? throw new InvalidOperationException("Byte encoding options must be an object");
    static string ByteEncodingOption(TsObject options, string key, string fallback, string first, string second, string third = null)
    {
        object value = options == null ? Undefined : options.Get(key);
        if (IsUndefined(value)) return fallback;
        if (value is not string text || text != first && text != second && text != third) throw new InvalidOperationException("Invalid byte encoding option");
        return text;
    }
    static bool Base64Space(char value) => value is ' ' or '\t' or '\n' or '\r' or '\f';
    static int Base64Digit(char value, bool url) => value is >= 'A' and <= 'Z' ? value - 'A' : value is >= 'a' and <= 'z' ? value - 'a' + 26 : value is >= '0' and <= '9' ? value - '0' + 52 : value == (url ? '-' : '+') ? 62 : value == (url ? '_' : '/') ? 63 : value == '=' ? 64 : -1;
    static (bool Url, string Mode) ByteBase64Options(object settings)
    {
        var options = ByteEncodingOptions(settings);
        bool url = ByteEncodingOption(options, "alphabet", "base64", "base64", "base64url") == "base64url";
        string mode = ByteEncodingOption(options, "lastChunkHandling", "loose", "loose", "strict", "stop-before-partial");
        return (url, mode);
    }
    static (int Read, int Written) DecodeByteBase64(string text, bool url, string mode, Span<byte> destination)
    {
        int position = 0, read = 0, written = 0;
        Span<int> chunk = stackalloc int[4];
        // Inspect the next chunk after a write before deciding it cannot fit.
        while (position < text.Length && (written < destination.Length || written != 0))
        {
            int count = 0, start = position, end = position;
            while (position < text.Length && count < 4)
            {
                char ch = text[position++]; if (Base64Space(ch)) continue;
                int digit = Base64Digit(ch, url); if (digit < 0) throw InvalidByteText();
                if (count == 0) start = position - 1;
                chunk[count++] = digit; end = position;
            }
            if (count == 0) break;
            if (chunk[0] == 64 || count > 1 && chunk[1] == 64) throw InvalidByteText();
            if (count < 4 && mode == "stop-before-partial") { read = start; break; }
            if (count == 1 || count < 4 && mode == "strict") throw InvalidByteText();
            int bytes = count - 1;
            bool padded = false;
            if (count == 4 && chunk[2] == 64) { if (chunk[3] != 64) throw InvalidByteText(); bytes = 1; padded = true; }
            else if (count == 4 && chunk[3] == 64) { bytes = 2; padded = true; }
            else if (count == 3 && chunk[2] == 64) throw InvalidByteText();
            if (mode == "strict" && (bytes == 1 && (chunk[1] & 15) != 0 || bytes == 2 && (chunk[2] & 3) != 0)) throw InvalidByteText();
            if (padded) while (position < text.Length) if (!Base64Space(text[position++])) throw InvalidByteText();
            if (bytes > destination.Length - written) break;
            destination[written++] = (byte)((chunk[0] << 2) | (chunk[1] >> 4));
            if (bytes > 1) destination[written++] = (byte)((chunk[1] << 4) | (chunk[2] >> 2));
            if (bytes > 2) destination[written++] = (byte)((chunk[2] << 6) | chunk[3]);
            read = end;
            if (padded) break;
        }
        return (read, written);
    }
    static object ByteReadResult(int read, int written)
    {
        var result = new JsDynamicObject(); result.DefineOwn("read", (double)read); result.DefineOwn("written", (double)written); return result;
    }
    static void DefineByteEncoding(NativeNamespaceFunction constructor, JsClassInstance prototype)
    {
        constructor.Member("fromHex", new JsFunction((FunctionBody)(static args =>
        {
            string text = ByteText(FunctionArgument(args, 0)); byte[] bytes;
            try { bytes = Convert.FromHexString(text); } catch (FormatException) { throw InvalidByteText(); }
            var result = AllocateTypedArray("Uint8Array", bytes.Length); bytes.CopyTo(result.Buffer.Bytes, 0); return result;
        }), null, "fromHex", 1));
        constructor.Member("fromBase64", new JsFunction((FunctionBody)(static args =>
        {
            string text = ByteText(FunctionArgument(args, 0));
            var options = ByteBase64Options(FunctionArgument(args, 1));
            var bytes = new byte[checked(text.Length / 4 * 3 + 3)];
            var decoded = DecodeByteBase64(text, options.Url, options.Mode, bytes);
            var result = AllocateTypedArray("Uint8Array", decoded.Written); bytes.AsSpan(0, decoded.Written).CopyTo(result.Buffer.Bytes); return result;
        }), null, "fromBase64", 1));
        prototype.DefineMethod("toHex", new CollectionMethod("toHex", 0, static (receiver, _) =>
        {
            var array = RequireByteArray(receiver); return Convert.ToHexStringLower(ByteArrayBytes(array));
        }));
        prototype.DefineMethod("toBase64", new CollectionMethod("toBase64", 0, static (receiver, args) =>
        {
            var array = RequireByteArray(receiver); var options = ByteEncodingOptions(FunctionArgument(args, 0));
            bool url = ByteEncodingOption(options, "alphabet", "base64", "base64", "base64url") == "base64url";
            bool omit = options != null && Truthy(options.Get("omitPadding"));
            string text = Convert.ToBase64String(ByteArrayBytes(array));
            if (omit) text = text.TrimEnd('=');
            return url ? text.Replace('+', '-').Replace('/', '_') : text;
        }));
        prototype.DefineMethod("setFromBase64", new CollectionMethod("setFromBase64", 1, static (receiver, args) =>
        {
            var array = RequireByteArray(receiver); string text = ByteText(FunctionArgument(args, 0));
            var options = ByteBase64Options(FunctionArgument(args, 1));
            var decoded = DecodeByteBase64(text, options.Url, options.Mode, ByteArrayBytes(array));
            return ByteReadResult(decoded.Read, decoded.Written);
        }));
        prototype.DefineMethod("setFromHex", new CollectionMethod("setFromHex", 1, static (receiver, args) =>
        {
            var array = RequireByteArray(receiver); string text = ByteText(FunctionArgument(args, 0));
            Span<byte> bytes = ByteArrayBytes(array);
            if (text.Length % 2 != 0) throw InvalidByteText();
            int count = Math.Min(text.Length / 2, bytes.Length);
            for (int i = 0; i < count; i++)
            {
                int high = UriHex(text[2 * i]), low = UriHex(text[2 * i + 1]); if (high < 0 || low < 0) throw InvalidByteText();
                bytes[i] = (byte)((high << 4) | low);
            }
            return ByteReadResult(count * 2, count);
        }));
    }
}
