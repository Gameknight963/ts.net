using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class ResponseValue : JsDescriptorObject
    {
        internal int Status = 200;
        internal string StatusText = "", Type = "default", Url = "";
        internal bool Redirected;
        internal HeadersValue Headers;
        internal BufferedBody Body;
        internal CustomReadableStream StreamBody;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)ResponseConstructors.Value.Get("prototype");
    }
    static ResponseValue RequireResponse(object receiver) => receiver as ResponseValue ?? throw new InvalidOperationException("Response receiver required");
    static ResponseValue CreateResponse(object[] args, string contentType = null)
    {
        object input = FunctionArgument(args,0);
        var stream = input as CustomReadableStream;
        if (stream != null && (stream.Disturbed || StreamLocked(stream))) throw new InvalidOperationException("Body stream has already been consumed or locked");
        var body = ExtractResponseBody(stream == null ? input : null);
        object options = FunctionArgument(args,1);
        if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Response options must be an object");
        var settings = options as TsObject;
        var result = new ResponseValue {Headers = CreateHeaders(settings == null ? Undefined : settings.Get("headers"))};
        object status = settings == null ? Undefined : settings.Get("status");
        result.Status = IsUndefined(status) ? 200 : unchecked((ushort)ToInt32(Number(status)));
        if (result.Status < 200 || result.Status > 599) throw new ArgumentOutOfRangeException("status","Response status must be 200-599");
        object text = settings == null ? Undefined : settings.Get("statusText");
        result.StatusText = IsUndefined(text) ? "" : HeaderByteString(text);
        foreach (char character in result.StatusText)
            if (character < 32 && character != '\t' || character == 127) throw new InvalidOperationException("Invalid response status text");
        if (body.Bytes != null || stream != null) {
            if (result.Status is 204 or 205 or 304) throw new InvalidOperationException("This response status cannot have a body");
            if (stream != null) result.StreamBody = stream;
            else result.Body = body.Stream ?? new BufferedBody(body.Bytes);
            string type = contentType ?? body.ContentType;
            if (type != null && !result.Headers.Fields.ContainsKey("content-type")) result.Headers.Append("content-type",type);
        }
        return result;
    }
    static HeadersValue CopyResponseHeaders(HeadersValue source)
    {
        var result = new HeadersValue {Immutable = source.Immutable};
        foreach (var pair in source.Fields) result.Fields.Add(pair.Key,new List<string>(pair.Value));
        return result;
    }
    static object CloneResponse(object receiver)
    {
        var source = RequireResponse(receiver);
        if (source.StreamBody != null) {
            if (source.StreamBody.Disturbed || StreamLocked(source.StreamBody)) throw new InvalidOperationException("Response body cannot be cloned after use or while locked");
            var branches = TeeCustomStream(source.StreamBody);
            source.StreamBody = (CustomReadableStream)Get(branches,"0");
            return new ResponseValue {
                Status=source.Status, StatusText=source.StatusText, Type=source.Type, Url=source.Url, Redirected=source.Redirected,
                Headers=CopyResponseHeaders(source.Headers), StreamBody=(CustomReadableStream)Get(branches,"1")
            };
        }
        if (source.Body != null && (source.Body.Disturbed || source.Body.Locked)) throw new InvalidOperationException("Response body cannot be cloned after use or while locked");
        source.Body?.Tee?.BranchRead();
        return new ResponseValue {
            Status = source.Status, StatusText = source.StatusText, Type = source.Type, Url = source.Url, Redirected = source.Redirected,
            Headers = CopyResponseHeaders(source.Headers), Body = source.Body == null ? null : new BufferedBody(source.Body.Bytes)
        };
    }
    static object ConsumeResponse(object receiver, string kind) => BodyPromiseOperation(() => {
        var response = RequireResponse(receiver);
        if (response.StreamBody != null) return ConsumeStreamBody(response.StreamBody,response.Headers,kind);
        return BodyOperation(() => ConsumeHttpBody(response.Body,response.Headers,kind));
    });
    static object ConsumeHttpBody(BufferedBody body, HeadersValue headers, string kind)
    {
        byte[] bytes = body?.Consume() ?? Array.Empty<byte>();
        return ConvertHttpBody(bytes,headers,kind);
    }
    static object ConvertHttpBody(byte[] bytes, HeadersValue headers, string kind)
    {
        if (kind == "text") return ResponseText(bytes);
        if (kind == "json") return new JsonParser(ResponseText(bytes),false).Parse().Value;
        if (kind == "formData") return ParseHttpFormData(bytes,headers);
        if (kind == "blob") {
            string type = headers.Fields.TryGetValue("content-type",out var values) ? HeadersValue.Joined("content-type",values) : "";
            if (!System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(type,out _)) type = "";
            return new BlobValue(bytes,BlobType(type));
        }
        if (kind == "bytes") {var array = AllocateTypedArray("Uint8Array",bytes.Length);bytes.CopyTo(array.Buffer.Bytes,0);return array;}
        var result = new BinaryBuffer(bytes.Length); bytes.CopyTo(result.Bytes,0); return result;
    }
    static class ResponseConstructors {internal static readonly JsFunction Value = new ResponseConstructor();}
    sealed class ResponseConstructor : NativeNamespaceFunction
    {
        internal ResponseConstructor() : base("Response",static _ => throw new InvalidOperationException("Response constructor requires new"),0)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol),"Response",false);
            void Getter(string name, Func<ResponseValue,object> read) => prototype.DefineAccessor(name,new CollectionMethod("get " + name,0,(receiver,_) => read(RequireResponse(receiver))),1);
            Getter("status",value => (double)value.Status); Getter("statusText",value => value.StatusText); Getter("ok",value => BoxBoolean(value.Status >= 200 && value.Status <= 299));
            Getter("headers",value => value.Headers); Getter("body",value => (object)value.StreamBody ?? value.Body); Getter("bodyUsed",value => BoxBoolean(value.StreamBody?.Disturbed ?? value.Body?.Disturbed ?? false));
            Getter("url",value => value.Url); Getter("redirected",value => BoxBoolean(value.Redirected)); Getter("type",value => value.Type);
            prototype.DefineOwn("clone",new CollectionMethod("clone",0,(receiver,_) => CloneResponse(receiver)));
            foreach (string kind in new[] {"text","json","arrayBuffer","blob","bytes","formData"})
                prototype.DefineOwn(kind,new CollectionMethod(kind,0,(receiver,_) => ConsumeResponse(receiver,kind)));
            Member("error",new JsFunction((FunctionBody)(static _ => new ResponseValue {Status=0,Type="error",Headers=new HeadersValue {Immutable=true}}),null,"error",0));
            Member("redirect",new JsFunction((FunctionBody)(static args => {
                if (args.Length == 0) throw new InvalidOperationException("Response.redirect requires a URL");
                string input = QueryText(args[0]);
                if (!TryUrl(input,null,out var url)) throw new InvalidOperationException("Redirect requires an absolute URL");
                object status = FunctionArgument(args,1); int code = IsUndefined(status) ? 302 : unchecked((ushort)ToInt32(Number(status)));
                if (code is not (301 or 302 or 303 or 307 or 308)) throw new ArgumentOutOfRangeException("status","Invalid redirect status");
                var result = new ResponseValue {Status=code,Headers=new HeadersValue()}; result.Headers.Append("location",url.AbsoluteUri); result.Headers.Immutable=true;return result;
            }),null,"redirect",1));
            Member("json",new JsFunction((FunctionBody)(static args => {
                if (args.Length == 0) throw new InvalidOperationException("Response.json requires data");
                object text = new JsonWriter(Undefined,Undefined).Write(args[0]);
                if (text is not string) throw new InvalidOperationException("Response.json data is not serializable");
                return CreateResponse(new object[] {text,FunctionArgument(args,1)},"application/json");
            }),null,"json",1));
        }
        public override object Construct(object[] args) => CreateResponse(args);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = CreateResponse(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
}
