using System;
using System.Globalization;

public static partial class Js
{
    static bool UrlAuthority(Uri uri) => uri.AbsoluteUri.AsSpan(uri.Scheme.Length + 1).StartsWith("//", StringComparison.Ordinal);
    static Uri CanonicalUrl(Uri uri)
    {
        if (UrlAuthority(uri) && uri.HostNameType == UriHostNameType.Dns && uri.Host != uri.IdnHost)
            uri = new UriBuilder(uri) { Host = uri.IdnHost }.Uri;
        if (uri.Scheme == "file" && uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            uri = new UriBuilder(uri) { Host = "" }.Uri;
        return uri;
    }
    // Parsing and canonicalization use the CLR URI implementation.
    static bool TryUrl(string input, string basis, out Uri result)
    {
        result = null;
        static bool Scheme(string text) { text = text.Trim(); int colon = text.IndexOf(':'); return colon > 0 && Uri.CheckSchemeName(text[..colon]); }
        if (basis == null && !Scheme(input) || basis != null && !Scheme(basis)) return false;
        try
        {
            if (basis != null)
            {
                if (!Uri.TryCreate(basis, UriKind.Absolute, out var parent)) return false;
                bool opaque = !UrlAuthority(parent) && !parent.AbsoluteUri.AsSpan(parent.Scheme.Length + 1).StartsWith("/", StringComparison.Ordinal);
                if (opaque && !Scheme(input) && !input.Trim().StartsWith('#')) return false;
                if (!Uri.TryCreate(parent, input, out result)) return false;
            }
            else if (!Uri.TryCreate(input, UriKind.Absolute, out result)) return false;
            result = CanonicalUrl(result);
            return true;
        }
        catch (Exception error) when (error is UriFormatException or ArgumentException) { result = null; return false; }
    }
    static string UrlEncode(string value, string allowed)
    {
        string result = Uri.EscapeDataString(value);
        foreach (char character in allowed)
            result = result.Replace("%" + ((int)character).ToString("X2", CultureInfo.InvariantCulture), character.ToString(), StringComparison.Ordinal);
        return result.Replace("%25", "%", StringComparison.Ordinal);
    }
    sealed class UrlValue : TsObject
    {
        Uri uri;
        internal readonly SearchParamsValue Params;
        internal UrlValue(Uri value)
        {
            uri = value; Params = CreateSearchParams(uri.Query); Params.Changed = UpdateQuery;
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RuntimeNamespaces.URL.Get("prototype");
        internal string Href => uri.AbsoluteUri;
        bool Authority => UrlAuthority(uri);
        string Hostname => !Authority ? "" : uri.HostNameType == UriHostNameType.IPv6 ? "[" + uri.IdnHost.Trim('[', ']') + "]" : uri.IdnHost;
        string Port => !Authority || uri.IsDefaultPort || uri.Port < 0 ? "" : uri.Port.ToString(CultureInfo.InvariantCulture);
        string Host => Hostname + (Port.Length == 0 ? "" : ":" + Port);
        string Pathname
        {
            get
            {
                if (!Authority)
                {
                    int start = uri.Scheme.Length + 1, end = Href.IndexOfAny(new[] { '?', '#' }, start);
                    return Href[start..(end < 0 ? Href.Length : end)];
                }
                string path = uri.AbsolutePath;
                return uri.Scheme == "file" && !path.StartsWith('/') ? "/" + path : path;
            }
        }
        string Username => !Authority ? "" : uri.UserInfo.Split(':', 2)[0];
        string Password { get { if (!Authority) return ""; int colon = uri.UserInfo.IndexOf(':'); return colon < 0 ? "" : uri.UserInfo[(colon + 1)..]; } }
        string Origin
        {
            get
            {
                if (uri.Scheme is "http" or "https" or "ftp" or "ws" or "wss") return uri.Scheme + "://" + Host;
                if (uri.Scheme == "blob" && TryUrl(Pathname, null, out var inner) && inner.Scheme is "http" or "https") return new UrlValue(inner).Origin;
                return "null";
            }
        }
        internal object Read(string key) => key switch
        {
            "href" => Href, "origin" => Origin, "protocol" => uri.Scheme + ":", "username" => Username,
            "password" => Password, "host" => Host, "hostname" => Hostname, "port" => Port, "pathname" => Pathname,
            "search" => uri.Query == "?" ? "" : uri.Query, "hash" => uri.Fragment == "#" ? "" : uri.Fragment,
            "searchParams" => Params, _ => Undefined
        };
        void Replace(Uri value)
        {
            string prior = uri.Query; uri = value;
            if (prior == uri.Query) return;
            var parsed = CreateSearchParams(uri.Query);
            Params.Pairs.Clear(); Params.Pairs.AddRange(parsed.Pairs);
        }
        string WithQuery(string query)
        {
            string text = Href; int hash = text.IndexOf('#'); string fragment = hash < 0 ? "" : text[hash..];
            if (hash >= 0) text = text[..hash];
            int question = text.IndexOf('?'); if (question >= 0) text = text[..question];
            return text + query + fragment;
        }
        void UpdateQuery()
        {
            string serialized = Params.Serialize();
            if (!TryUrl(WithQuery(serialized.Length == 0 ? "" : "?" + serialized), null, out var value)) throw new InvalidOperationException("Invalid URL query");
            uri = value;
        }
        static bool Special(string scheme) => scheme is "http" or "https" or "ftp" or "file" or "ws" or "wss";
        internal object Write(string key, string value)
        {
            if (key == "href")
            {
                if (!TryUrl(value, null, out var parsed)) throw new InvalidOperationException("Invalid URL");
                Replace(parsed); return Undefined;
            }
            if (key == "search")
            {
                string query = value.StartsWith('?') ? value[1..] : value;
                query = value.Length == 0 ? "" : "?" + UrlEncode(query, "/?:@!$&'()*+,;=|[]");
                if (TryUrl(WithQuery(query), null, out var parsed)) Replace(parsed);
                return Undefined;
            }
            if (key == "hash")
            {
                string text = Href; int hash = text.IndexOf('#'); if (hash >= 0) text = text[..hash];
                string fragment = value.StartsWith('#') ? value[1..] : value;
                if (value.Length != 0) text += "#" + UrlEncode(fragment, "/?:@!$&'()*+,;=|[]#");
                if (TryUrl(text, null, out var parsed)) Replace(parsed);
                return Undefined;
            }
            try
            {
                var builder = new UriBuilder(uri);
                switch (key)
                {
                    case "protocol":
                        string scheme = value.TrimEnd(':').ToLowerInvariant();
                        if (!Uri.CheckSchemeName(scheme) || Special(scheme) != Special(uri.Scheme)) return Undefined;
                        if (scheme == "file" && (uri.UserInfo.Length != 0 || Port.Length != 0)) return Undefined;
                        if (uri.IsDefaultPort) builder.Port = -1;
                        builder.Scheme = scheme; break;
                    case "username": case "password":
                        if (!Authority || uri.Scheme == "file") return Undefined;
                        string user = key == "username" ? UrlEncode(value, "!$&'()*+,") : Username;
                        string password = key == "password" ? UrlEncode(value, "!$&'()*+,") : Password;
                        string credentials = user + (password.Length == 0 ? "" : ":" + password);
                        if (credentials.Length != 0) credentials += "@";
                        if (TryUrl(uri.Scheme + "://" + credentials + Host + Pathname + uri.Query + uri.Fragment, null, out var credentialUrl)) Replace(credentialUrl);
                        return Undefined;
                    case "host":
                        if (!Authority) return Undefined;
                        if (!TryUrl(uri.Scheme + "://" + value + "/", null, out var host) || host.UserInfo.Length != 0) return Undefined;
                        builder.Host = host.IdnHost;
                        string authority = value.Split(new[] { '/', '?', '#' }, 2)[0];
                        int separator = authority.StartsWith('[') ? authority.IndexOf(':', authority.IndexOf(']') + 1) : authority.LastIndexOf(':');
                        if (separator >= 0 && separator + 1 < authority.Length) builder.Port = host.IsDefaultPort ? -1 : host.Port;
                        break;
                    case "hostname": if (!Authority) return Undefined; builder.Host = value; break;
                    case "port":
                        if (!Authority || uri.Scheme == "file") return Undefined;
                        if (value.Length == 0) builder.Port = -1;
                        else if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int port) && port <= 65535) builder.Port = port;
                        else return Undefined;
                        break;
                    case "pathname":
                        if (!Authority && !Pathname.StartsWith('/')) return Undefined;
                        builder.Path = UrlEncode(value, "/:@!$&'()*+,;=|[]"); break;
                }
                Replace(CanonicalUrl(builder.Uri));
            }
            catch (Exception error) when (error is UriFormatException or ArgumentException) { }
            return Undefined;
        }
    }
    static UrlValue RequireUrl(object receiver) => receiver as UrlValue ?? throw new InvalidOperationException("URL receiver required");
    static bool TryUrlArguments(object[] args, out Uri uri)
    {
        if (args.Length == 0) throw new InvalidOperationException("URL requires an address");
        string input = QueryText(args[0]);
        string basis = IsUndefined(FunctionArgument(args, 1)) ? null : QueryText(args[1]);
        return TryUrl(input, basis, out uri);
    }
    static UrlValue UrlArguments(object[] args, bool required)
    {
        if (TryUrlArguments(args, out var uri)) return new UrlValue(uri);
        if (required) throw new InvalidOperationException("Invalid URL");
        return null;
    }
    sealed class UrlConstructor : NativeNamespaceFunction
    {
        internal UrlConstructor() : base("URL", static _ => throw new InvalidOperationException("URL requires new"), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "URL");
            foreach (string key in new[] { "href", "origin", "protocol", "username", "password", "host", "hostname", "port", "pathname", "search", "hash", "searchParams" })
            {
                prototype.DefineClassAccessor(key, new CollectionMethod("get " + key, 0, (receiver, _) => RequireUrl(receiver).Read(key)), 1);
                if (key is not ("origin" or "searchParams")) prototype.DefineClassAccessor(key, new CollectionMethod("set " + key, 1, (receiver, args) => RequireUrl(receiver).Write(key, QueryText(FunctionArgument(args, 0)))), 2);
            }
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, (receiver, _) => RequireUrl(receiver).Href));
            prototype.DefineMethod("toJSON", new CollectionMethod("toJSON", 0, (receiver, _) => RequireUrl(receiver).Href));
            Member("canParse", new JsFunction((FunctionBody)(static args => BoxBoolean(TryUrlArguments(args, out _))), null, "canParse", 1));
            Member("parse", new JsFunction((FunctionBody)(static args => UrlArguments(args, false)), null, "parse", 1));
        }
        public override object Construct(object[] args) => UrlArguments(args, true);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
