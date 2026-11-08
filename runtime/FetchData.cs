using System;
using System.Collections.Generic;
using System.Text;

public static partial class Js
{
    static string DataRequestUrl(Uri uri)
    {
        string text = uri.OriginalString;
        int start = 0, end = text.Length;
        while (start < end && text[start] <= ' ') start++;
        while (end > start && text[end - 1] <= ' ') end--;
        text = text[start..end].Replace("\t", "").Replace("\r", "").Replace("\n", "");
        var result = new StringBuilder("data:");
        foreach (var rune in text[(text.IndexOf(':') + 1)..].EnumerateRunes())
            if (rune.Value < 0x20 || rune.Value > 0x7e) result.Append(Uri.EscapeDataString(rune.ToString()));
            else result.Append((char)rune.Value);
        return result.ToString();
    }
    static void FetchData(RequestValue request, JsPromise promise)
    {
        // Keep completion on the entry thread, and allow an immediate abort to win.
        EnqueuePromiseJob(() =>
        {
            if (request.Signal.Aborted) { promise.Settle(request.Signal.Reason, true); return; }
            try
            {
                string url = request.Url.Split('#')[0];
                int comma = url.IndexOf(',');
                if (comma < 0) throw new InvalidOperationException("Invalid data URL");
                string type = url[5..comma].Trim(' ');
                byte[] encoded = Encoding.UTF8.GetBytes(url[(comma + 1)..]);
                var decoded = new List<byte>(encoded.Length);
                for (int index = 0; index < encoded.Length; index++)
                {
                    if (encoded[index] == '%' && index + 2 < encoded.Length && UriHex((char)encoded[index + 1]) >= 0 && UriHex((char)encoded[index + 2]) >= 0)
                    { decoded.Add((byte)((UriHex((char)encoded[index + 1]) << 4) | UriHex((char)encoded[index + 2]))); index += 2; }
                    else decoded.Add(encoded[index]);
                }
                byte[] bytes = decoded.ToArray();
                if (type.EndsWith("base64", StringComparison.OrdinalIgnoreCase))
                {
                    string prefix = type[..^6].TrimEnd(' ');
                    if (prefix.EndsWith(';'))
                    {
                        type = prefix[..^1];
                        try { bytes = Encoding.Latin1.GetBytes(DecodeBase64(Encoding.Latin1.GetString(bytes))); }
                        catch { throw new InvalidOperationException("Invalid data URL base64"); }
                    }
                }
                if (type.StartsWith(';')) type = "text/plain" + type;
                type = DataMimeType(type);
                if (request.Method == "HEAD") bytes = null;
                CheckFetchIntegrity(request.Integrity, bytes);
                var headers = new HeadersValue(); headers.Append("content-type", type); headers.Immutable = true;
                promise.ResolveValue(new ResponseValue { StatusText = "OK", Type = "basic", Url = url, Headers = headers, Body = bytes == null ? null : new BufferedBody(bytes) });
            }
            catch (Exception) { promise.Settle(new JsCaughtError("TypeError", "fetch failed"), true); }
        });
    }
    static bool DataMimeToken(string value)
    {
        if (value.Length == 0) return false;
        foreach (char character in value)
            if (!char.IsAsciiLetterOrDigit(character) && "!#$%&'*+-.^_`|~".IndexOf(character) < 0) return false;
        return true;
    }
    static string DataMimeType(string text)
    {
        int semicolon = text.IndexOf(';');
        string essence = (semicolon < 0 ? text : text[..semicolon]).TrimEnd(' ', '\t', '\r', '\n');
        int slash = essence.IndexOf('/');
        if (slash < 0 || !DataMimeToken(essence[..slash]) || !DataMimeToken(essence[(slash + 1)..])) return "text/plain;charset=US-ASCII";
        var result = new StringBuilder(essence.ToLowerInvariant());
        var names = new HashSet<string>(StringComparer.Ordinal);
        int position = semicolon < 0 ? text.Length : semicolon;
        while (position < text.Length)
        {
            position++;
            while (position < text.Length && text[position] is ' ' or '\t' or '\r' or '\n') position++;
            int start = position;
            while (position < text.Length && text[position] is not (';' or '=')) position++;
            string name = text[start..position].ToLowerInvariant();
            if (position == text.Length || text[position] == ';') continue;
            position++;
            string value;
            if (position < text.Length && text[position] == '"')
            {
                position++; var builder = new StringBuilder();
                while (position < text.Length)
                {
                    char character = text[position++];
                    if (character == '"') break;
                    if (character == '\\' && position < text.Length) character = text[position++];
                    builder.Append(character);
                }
                value = builder.ToString();
                while (position < text.Length && text[position] != ';') position++;
            }
            else
            {
                start = position; while (position < text.Length && text[position] != ';') position++;
                value = text[start..position].TrimEnd(' ', '\t', '\r', '\n');
                if (value.Length == 0) continue;
            }
            bool valid = true;
            foreach (char character in value) if (character != '\t' && (character < ' ' || character > '~') && character is not (>= '\u0080' and <= '\u00ff')) valid = false;
            if (!valid || !DataMimeToken(name) || !names.Add(name)) continue;
            result.Append(';').Append(name).Append('=');
            if (DataMimeToken(value)) result.Append(value);
            else result.Append('"').Append(value.Replace("\\", "\\\\").Replace("\"", "\\\"")).Append('"');
        }
        return result.ToString();
    }
}
