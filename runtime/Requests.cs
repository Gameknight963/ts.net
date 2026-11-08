using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class RequestValue : TsObject
    {
        internal string Url, Method = "GET", Referrer = "about:client", ReferrerPolicy = "", Mode = "cors", Credentials = "same-origin", Cache = "default", Redirect = "follow", Integrity = "";
        internal bool KeepAlive, StreamBody;
        internal HeadersValue Headers;
        internal BufferedBody Body;
        internal AbortSignalValue Signal;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RequestConstructors.Value.Get("prototype");
    }
    static RequestValue RequireRequest(object receiver) => receiver as RequestValue ?? throw new InvalidOperationException("Request receiver required");
    static object RequestSetting(TsObject options, string name) => options == null ? Undefined : options.Get(name);
    static string RequestChoice(TsObject options, string name, string fallback, params ReadOnlySpan<string> allowed)
    {
        object input = RequestSetting(options,name);
        if (IsUndefined(input)) return fallback;
        string value = StringValue(input);
        if (allowed.IndexOf(value) < 0) throw new InvalidOperationException("Invalid request " + name);
        return value;
    }
    // Use the existing dependency graph, which marks linked states before callbacks.
    static AbortSignalValue FollowRequestSignal(AbortSignalValue source)
    {
        var signal = new AbortSignalValue();
        if (source == null) return signal;
        if (source.Aborted) signal.Abort(source.Reason);
        else { signal.Sources = new List<AbortSignalValue> {source}; source.AddDependent(signal); }
        return signal;
    }
    static RequestValue CopyRequest(RequestValue source)
    {
        return new RequestValue {
            Url=source.Url, Method=source.Method, Referrer=source.Referrer, ReferrerPolicy=source.ReferrerPolicy, Mode=source.Mode,
            Credentials=source.Credentials, Cache=source.Cache, Redirect=source.Redirect, Integrity=source.Integrity,
            KeepAlive=source.KeepAlive, StreamBody=source.StreamBody, Headers=CopyResponseHeaders(source.Headers)
        };
    }
    static RequestValue CreateRequest(object[] args)
    {
        if (args.Length == 0) throw new InvalidOperationException("Request requires a URL or request");
        object input = args[0]; var source = input as RequestValue;
        RequestValue result;
        if (source != null) {result = CopyRequest(source); result.Referrer = "about:client"; result.ReferrerPolicy = "";}
        else {
            if (!TryUrl(QueryText(input),null,out var uri) || uri.UserInfo.Length != 0) throw new InvalidOperationException("Request requires an absolute URL without credentials");
            result = new RequestValue {Url=uri.AbsoluteUri,Headers=new HeadersValue()};
        }
        object settings = FunctionArgument(args,1);
        if (!IsNullish(settings) && settings is not TsObject) throw new InvalidOperationException("Request options must be an object");
        var options = settings as TsObject;
        object headers = RequestSetting(options,"headers"); if (!IsUndefined(headers)) result.Headers = CreateHeaders(headers);
        object method = RequestSetting(options,"method");
        if (!IsUndefined(method)) {
            result.Method = HeaderByteString(method); HeaderName(result.Method);
            string normalized = result.Method.ToUpperInvariant();
            if (normalized is "CONNECT" or "TRACE" or "TRACK") throw new InvalidOperationException("Forbidden request method");
            if (normalized is "DELETE" or "GET" or "HEAD" or "OPTIONS" or "POST" or "PUT") result.Method = normalized;
        }
        result.Mode = RequestChoice(options,"mode",result.Mode,"cors","same-origin","no-cors","navigate");
        if (result.Mode == "navigate") throw new InvalidOperationException("Request constructor cannot use navigate mode");
        if (result.Mode == "no-cors" && result.Method is not ("GET" or "HEAD" or "POST")) throw new InvalidOperationException("Method is not allowed in no-cors mode");
        result.Credentials = RequestChoice(options,"credentials",result.Credentials,"omit","same-origin","include");
        result.Cache = RequestChoice(options,"cache",result.Cache,"default","no-store","reload","no-cache","force-cache","only-if-cached");
        if (result.Cache == "only-if-cached" && result.Mode != "same-origin") throw new InvalidOperationException("only-if-cached requires same-origin mode");
        result.Redirect = RequestChoice(options,"redirect",result.Redirect,"follow","error","manual");
        result.ReferrerPolicy = RequestChoice(options,"referrerPolicy",result.ReferrerPolicy,"","no-referrer","no-referrer-when-downgrade","same-origin","origin","strict-origin","origin-when-cross-origin","strict-origin-when-cross-origin","unsafe-url");
        object referrer = RequestSetting(options,"referrer");
        if (!IsUndefined(referrer)) {
            string text = QueryText(referrer);
            if (text is "" or "about:client") result.Referrer = text;
            else if (TryUrl(text,null,out var uri)) result.Referrer = uri.AbsoluteUri;
            else throw new InvalidOperationException("Invalid request referrer");
        }
        object integrity = RequestSetting(options,"integrity"); if (!IsUndefined(integrity)) result.Integrity = StringValue(integrity);
        object keepAlive = RequestSetting(options,"keepalive"); if (!IsUndefined(keepAlive)) result.KeepAlive = Truthy(keepAlive);
        object duplex = RequestSetting(options,"duplex");
        if (!IsUndefined(duplex) && StringValue(duplex) != "half") throw new InvalidOperationException("Invalid request duplex");
        object window = RequestSetting(options,"window"); if (!IsUndefined(window) && window != null) throw new InvalidOperationException("Request window must be null");
        object signalValue = RequestSetting(options,"signal");
        AbortSignalValue signal = source?.Signal;
        if (!IsUndefined(signalValue)) signal = signalValue == null ? null : RequireAbortSignal(signalValue);
        object bodyValue = RequestSetting(options,"body"); bool overrideBody = !IsNullish(bodyValue);
        if (overrideBody) {
            var body = ExtractResponseBody(bodyValue);
            result.StreamBody = body.Stream != null;
            if (result.StreamBody && IsUndefined(duplex)) throw new InvalidOperationException("Stream request bodies require duplex: half");
            result.Body = body.Stream ?? new BufferedBody(body.Bytes);
            if (body.ContentType != null && !result.Headers.Fields.ContainsKey("content-type")) result.Headers.Append("content-type",body.ContentType);
        } else if (source?.Body != null) {
            if (source.Body.Disturbed || source.Body.Locked) throw new InvalidOperationException("Source request body has already been consumed or locked");
            result.Body = new BufferedBody(source.Body.Bytes);
        }
        if (result.Body != null && result.Method is "GET" or "HEAD") throw new InvalidOperationException("GET and HEAD requests cannot have a body");
        if (result.StreamBody && (result.KeepAlive || result.Mode == "no-cors")) throw new InvalidOperationException("Stream request bodies cannot use keepalive or no-cors mode");
        result.Signal = FollowRequestSignal(signal);
        // Ownership transfers only after validation succeeds. Buffered bytes can
        // be shared safely while the old request becomes unusable for reading.
        if (!overrideBody && source?.Body != null) {source.Body.Disturbed=true;source.Body.Consuming=true;source.Body.Closed=true;}
        return result;
    }
    static object CloneRequest(object receiver)
    {
        var source = RequireRequest(receiver);
        if (source.Body != null && (source.Body.Disturbed || source.Body.Locked)) throw new InvalidOperationException("Request body cannot be cloned after use or while locked");
        var result = CopyRequest(source);
        source.Body?.Tee?.BranchRead();
        result.Body = source.Body == null ? null : new BufferedBody(source.Body.Bytes);
        result.Signal = FollowRequestSignal(source.Signal);
        return result;
    }
    static object ConsumeRequest(object receiver, string kind) => BodyOperation(() => {
        var request = RequireRequest(receiver); return ConsumeHttpBody(request.Body,request.Headers,kind);
    });
    static class RequestConstructors {internal static readonly JsFunction Value = new RequestConstructor();}
    sealed class RequestConstructor : NativeNamespaceFunction
    {
        internal RequestConstructor() : base("Request",static _ => throw new InvalidOperationException("Request constructor requires new"),1)
        {
            var prototype = new JsClassInstance {ParentPrototype=DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol),"Request");
            void Getter(string name, Func<RequestValue,object> read) => prototype.DefineAccessor(name,new CollectionMethod("get " + name,0,(receiver,_) => read(RequireRequest(receiver))),1);
            Getter("url",value=>value.Url); Getter("method",value=>value.Method); Getter("headers",value=>value.Headers);
            Getter("body",value=>value.Body); Getter("bodyUsed",value=>BoxBoolean(value.Body?.Disturbed ?? false)); Getter("signal",value=>value.Signal);
            Getter("referrer",value=>value.Referrer); Getter("referrerPolicy",value=>value.ReferrerPolicy); Getter("mode",value=>value.Mode);
            Getter("credentials",value=>value.Credentials); Getter("cache",value=>value.Cache); Getter("redirect",value=>value.Redirect);
            Getter("integrity",value=>value.Integrity); Getter("keepalive",value=>BoxBoolean(value.KeepAlive)); Getter("destination",_=>""); Getter("duplex",_=>"half");
            Getter("isReloadNavigation",_=>BoxBoolean(false)); Getter("isHistoryNavigation",_=>BoxBoolean(false));
            prototype.DefineOwn("clone",new CollectionMethod("clone",0,(receiver,_)=>CloneRequest(receiver)));
            foreach (string kind in new[] {"text","json","arrayBuffer","blob","bytes","formData"})
                prototype.DefineOwn(kind,new CollectionMethod(kind,0,(receiver,_)=>ConsumeRequest(receiver,kind)));
        }
        public override object Construct(object[] args) => CreateRequest(args);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = CreateRequest(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
}
