using System;
using System.Collections.Generic;
using System.Globalization;

public static partial class Js
{
    [ThreadStatic] static Dictionary<string, BlobValue> objectUrls;
    static void ClearObjectUrls() => objectUrls?.Clear();
    static object CreateObjectUrl(object[] args)
    {
        var blob = RequireBlob(FunctionArgument(args, 0));
        string url = "blob:nodedata:" + Guid.NewGuid().ToString();
        (objectUrls ??= new(StringComparer.Ordinal)).Add(url, blob);
        return url;
    }
    static object RevokeObjectUrl(object[] args)
    {
        string url = StringValue(FunctionArgument(args, 0));
        objectUrls?.Remove(url.Split('#')[0]);
        return Undefined;
    }
    static void FetchBlob(RequestValue request, JsPromise promise)
    {
        string url = request.Url.Split('#')[0];
        BlobValue blob = null;
        objectUrls?.TryGetValue(url, out blob);
        EnqueuePromiseJob(() =>
        {
            if (request.Signal.Aborted) { promise.Settle(request.Signal.Reason, true); return; }
            try
            {
                if (blob == null || request.Method != "GET") throw new InvalidOperationException("Invalid blob fetch");
                int start = 0, count = blob.Bytes.Length;
                bool partial = false;
                if (request.Headers.Fields.TryGetValue("range", out var range))
                {
                    (start, count) = BlobFetchRange(HeadersValue.Joined("range", range), count);
                    partial = true;
                }
                var bytes = start == 0 && count == blob.Bytes.Length ? blob.Bytes : blob.Bytes.AsSpan(start, count).ToArray();
                CheckFetchIntegrity(request.Integrity, bytes);
                var headers = new HeadersValue();
                headers.Append("content-type", blob.Type);
                headers.Append("content-length", count.ToString(CultureInfo.InvariantCulture));
                if (partial) headers.Append("content-range", "bytes " + start.ToString(CultureInfo.InvariantCulture) + "-" + (start + count - 1).ToString(CultureInfo.InvariantCulture) + "/" + blob.Bytes.Length.ToString(CultureInfo.InvariantCulture));
                headers.Immutable = true;
                promise.ResolveValue(new ResponseValue { Status = partial ? 206 : 200, StatusText = partial ? "Partial Content" : "OK", Type = "basic", Url = url, Headers = headers, Body = new BufferedBody(bytes) });
            }
            catch (Exception) { promise.Settle(new JsCaughtError("TypeError", "fetch failed"), true); }
        });
    }
    static (int Start, int Count) BlobFetchRange(string text, int length)
    {
        if (!text.StartsWith("bytes=", StringComparison.Ordinal) || text.Contains(',')) throw new InvalidOperationException("Invalid blob range");
        string range = text[6..].Trim(' ', '\t'); int dash = range.IndexOf('-');
        if (dash < 0 || length == 0) throw new InvalidOperationException("Invalid blob range");
        static ulong Number(string value) => ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : throw new InvalidOperationException("Invalid blob range");
        if (dash == 0)
        {
            ulong suffix = Number(range[1..].Trim(' ', '\t'));
            int count = (int)Math.Min(suffix, (ulong)length);
            return (length - count, count);
        }
        ulong first = Number(range[..dash].Trim(' ', '\t'));
        string end = range[(dash + 1)..].Trim(' ', '\t');
        ulong last = end.Length == 0 ? (ulong)length - 1 : Number(end);
        if (first >= (ulong)length || last < first) throw new InvalidOperationException("Invalid blob range");
        last = Math.Min(last, (ulong)length - 1);
        return ((int)first, (int)(last - first + 1));
    }
}
