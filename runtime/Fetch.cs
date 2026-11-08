using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static partial class Js
{
    sealed record FetchInput(Uri Url, string Method, byte[] Body, List<KeyValuePair<string, string[]>> Headers, string Redirect, string Integrity);
    sealed record FetchOutput(int Status, string StatusText, string Url, bool Redirected, List<KeyValuePair<string, string[]>> Headers, byte[] Body);
    static class FetchFunctions
    {
        internal static readonly JsFunction Value = new((FunctionBody)Fetch, null, "fetch", 1);
    }
    static class FetchTransport
    {
        internal static readonly HttpClient Client = new(new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.None,
            RequestHeaderEncodingSelector = static (_, _) => Encoding.Latin1
        })
        { Timeout = Timeout.InfiniteTimeSpan };
    }
    static object Fetch(object[] args)
    {
        var result = new JsPromise();
        try
        {
            var request = CreateRequest(args);
            if (request.Signal.Aborted) { result.Settle(request.Signal.Reason, true); return result; }
            var uri = new Uri(request.Url);
            if (uri.Scheme == "data") { FetchData(request, result); return result; }
            if (uri.Scheme is not ("http" or "https")) throw new InvalidOperationException("Fetch currently supports HTTP and HTTPS URLs");
            var headers = new List<KeyValuePair<string, string[]>>();
            foreach (var header in request.Headers.Fields) headers.Add(new(header.Key, header.Value.ToArray()));
            byte[] body = request.Body?.Consume();
            var input = new FetchInput(new UriBuilder(uri) { Fragment = "" }.Uri, request.Method, body, headers, request.Redirect, request.Integrity);
            new FetchWork(result, request.Signal, CaptureHostEvents()).Start(input);
        }
        catch (Exception error) { result.Settle(CaughtValue(error), true); }
        return result;
    }
    sealed class FetchWork(JsPromise promise, AbortSignalValue signal, HostEventInbox inbox)
    {
        readonly CancellationTokenSource cancellation = new();
        Action unsubscribe;
        bool finished;
        internal void Start(FetchInput input)
        {
            inbox.Register(Shutdown, referenced: true);
            unsubscribe = signal.Subscribe(Abort);
            if (finished) { unsubscribe(); return; }
            _ = FetchHttp(input, cancellation.Token).ContinueWith(task =>
            {
                _ = task.Exception; // Observe failures even if the caller has already aborted.
                inbox.Post(() => Complete(task));
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        }
        void Release()
        {
            inbox.Unregister(Shutdown); unsubscribe?.Invoke(); cancellation.Dispose();
        }
        void Abort()
        {
            if (finished) return;
            finished = true; cancellation.Cancel();
            promise.Settle(signal.Reason, true); Release();
        }
        void Shutdown()
        {
            if (finished) return;
            finished = true; cancellation.Cancel(); Release();
        }
        void Complete(Task<FetchOutput> task)
        {
            if (finished) return;
            finished = true;
            try
            {
                if (!task.IsCompletedSuccessfully) { promise.Settle(new JsCaughtError("TypeError", "fetch failed"), true); return; }
                var output = task.Result;
                var headers = new HeadersValue();
                foreach (var item in output.Headers) headers.Fields.Add(item.Key.ToLowerInvariant(), new List<string>(item.Value));
                headers.Immutable = true;
                promise.ResolveValue(new ResponseValue
                {
                    Status = output.Status,
                    StatusText = output.StatusText,
                    Url = output.Url,
                    Redirected = output.Redirected,
                    Type = "basic",
                    Headers = headers,
                    Body = output.Body == null ? null : new BufferedBody(output.Body)
                });
            }
            catch (Exception error) { promise.Settle(CaughtValue(error), true); }
            finally { Release(); }
        }
    }
    static bool SameFetchOrigin(Uri first, Uri second) => first.Scheme == second.Scheme && first.IdnHost == second.IdnHost && first.Port == second.Port;
    static async Task<FetchOutput> FetchHttp(FetchInput input, CancellationToken cancellation)
    {
        Uri url = input.Url; string method = input.Method; byte[] body = input.Body;
        var headers = new List<KeyValuePair<string, string[]>>(input.Headers);
        for (int redirects = 0; ; redirects++)
        {
            using var message = new HttpRequestMessage(new HttpMethod(method), url);
            if (body != null) message.Content = new ByteArrayContent(body);
            foreach (var item in headers)
            {
                if (!message.Headers.TryAddWithoutValidation(item.Key, item.Value))
                {
                    message.Content ??= new ByteArrayContent(Array.Empty<byte>());
                    message.Content.Headers.TryAddWithoutValidation(item.Key, item.Value);
                }
            }
            if (!message.Headers.Contains("accept")) message.Headers.TryAddWithoutValidation("accept", "*/*");
            if (!message.Headers.Contains("accept-encoding")) message.Headers.TryAddWithoutValidation("accept-encoding", "gzip, deflate, br");
            using var response = await FetchTransport.Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
            int status = (int)response.StatusCode;
            bool redirect = status is 301 or 302 or 303 or 307 or 308;
            Uri location = response.Headers.Location;
            if (redirect && location != null && input.Redirect != "manual")
            {
                if (input.Redirect == "error" || redirects == 20) throw new HttpRequestException("Fetch redirect is not permitted");
                var next = new Uri(url, location);
                if (next.Scheme is not ("http" or "https") || next.UserInfo.Length != 0) throw new HttpRequestException("Invalid fetch redirect URL");
                if (!SameFetchOrigin(url, next)) headers.RemoveAll(item => item.Key is "authorization" or "proxy-authorization" or "cookie" or "cookie2");
                if (status is 301 or 302 && method == "POST" || status == 303 && method is not ("GET" or "HEAD"))
                {
                    method = "GET"; body = null;
                    headers.RemoveAll(item => item.Key.StartsWith("content-", StringComparison.OrdinalIgnoreCase));
                }
                url = new UriBuilder(next) { Fragment = "" }.Uri; continue;
            }
            var resultHeaders = new List<KeyValuePair<string, string[]>>();
            foreach (var item in response.Headers) resultHeaders.Add(new(item.Key, new List<string>(item.Value).ToArray()));
            foreach (var item in response.Content.Headers) resultHeaders.Add(new(item.Key, new List<string>(item.Value).ToArray()));
            byte[] bytes = null;
            if (method != "HEAD" && status is not (204 or 205 or 304))
            {
                bytes = await response.Content.ReadAsByteArrayAsync(cancellation).ConfigureAwait(false);
                foreach (string coding in ReverseFetchCodings(response.Content.Headers.ContentEncoding))
                {
                    using var memory = new MemoryStream(bytes, false);
                    using Stream decoded = coding switch
                    {
                        "gzip" or "x-gzip" => new GZipStream(memory, CompressionMode.Decompress),
                        "deflate" => new ZLibStream(memory, CompressionMode.Decompress),
                        "br" => new BrotliStream(memory, CompressionMode.Decompress),
                        _ => null
                    };
                    if (decoded == null) break;
                    using var output = new MemoryStream(); await decoded.CopyToAsync(output, cancellation).ConfigureAwait(false); bytes = output.ToArray();
                }
            }
            CheckFetchIntegrity(input.Integrity, bytes);
            return new FetchOutput(status, response.ReasonPhrase ?? "", url.AbsoluteUri, redirects != 0, resultHeaders, bytes);
        }
    }
    static IEnumerable<string> ReverseFetchCodings(IEnumerable<string> values)
    {
        var result = new List<string>(); foreach (string value in values) foreach (string item in value.Split(',')) result.Add(item.Trim().ToLowerInvariant());
        result.Reverse(); return result;
    }
}
