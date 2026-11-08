using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class HeadersValue : TsObject
    {
        internal readonly SortedDictionary<string, List<string>> Fields = new(StringComparer.Ordinal);
        List<KeyValuePair<string,string>> rows;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)HeaderConstructors.Value.Get("prototype");
        internal List<KeyValuePair<string,string>> Rows {
            get {
                if (rows != null) return rows;
                var result = new List<KeyValuePair<string,string>>();
                foreach (var group in Fields) {
                    if (group.Key == "set-cookie") foreach (string value in group.Value) result.Add(new(group.Key,value));
                    else result.Add(new(group.Key,Joined(group.Key,group.Value)));
                }
                return rows = result;
            }
        }
        internal static string Joined(string name, List<string> values) => values.Count == 1 ? values[0] : string.Join(name == "cookie" ? "; " : ", ",values);
        internal void Append(string name, string value, bool replace = false)
        {
            name = HeaderName(name); value = HeaderValue(value);
            if (replace || !Fields.TryGetValue(name,out var values)) Fields[name] = new List<string> {value};
            else values.Add(value);
            rows = null;
        }
        internal void Remove(string name) { if (Fields.Remove(name)) rows = null; }
    }
    static HeadersValue RequireHeaders(object receiver) => receiver as HeadersValue ?? throw new InvalidOperationException("Headers receiver required");
    static void HeaderArguments(object[] arguments, int count) { if (arguments.Length < count) throw new InvalidOperationException("Missing Headers argument"); }
    static string HeaderByteString(object value)
    {
        string text = StringValue(value);
        foreach (char character in text) if (character > 255) throw new InvalidOperationException("Headers require byte strings");
        return text;
    }
    static string HeaderName(string name)
    {
        if (name.Length == 0) throw new InvalidOperationException("Header name is empty");
        foreach (char character in name)
            if (!char.IsAsciiLetterOrDigit(character) && "!#$%&'*+-.^_`|~".IndexOf(character) < 0) throw new InvalidOperationException("Invalid header name");
        return name.ToLowerInvariant();
    }
    static string HeaderValue(string value)
    {
        value = value.Trim(' ','\t','\r','\n');
        if (value.AsSpan().IndexOfAny('\0','\r','\n') >= 0) throw new InvalidOperationException("Invalid header value");
        return value;
    }
    static HeadersValue CreateHeaders(object input)
    {
        var result = new HeadersValue();
        if (IsUndefined(input)) return result;
        if (input is not TsObject source) throw new InvalidOperationException("Headers initializer must be an object");
        var pairs = new List<string[]>();
        bool custom = TryStartCustomIterable(input,out var cursor);
        if (custom || input is JsArray or JsArguments or JsKeyedCollection or JsCollectionIterator or JsSequenceIterator or TypedArrayValue or JsBoxedPrimitive {Value: string} || IsIteratorObject(input)) {
            cursor = custom ? cursor : Iteration.Start(input);
            int position = 0;
            while (position < Iteration.Length(cursor)) {
                object pair = Iteration.Value(cursor,position); position += Iteration.Step(cursor,position);
                if (pair is not TsObject) throw new InvalidOperationException("Header entries must be iterable pairs");
                object values = Iteration.Start(pair); int index = 0;
                var items = new List<string>();
                while (index < Iteration.Length(values)) {
                    items.Add(HeaderByteString(Iteration.Value(values,index))); index += Iteration.Step(values,index);
                }
                pairs.Add(items.ToArray());
            }
        } else {
            var keys = (JsArray)source.OwnPropertyNames();
            for (uint index = 0; index < keys.Length(); index++) {
                string key = (string)keys.ElementAt(index);
                if (!source.HasOwn(key)) continue;
                if (IsSymbolKey(key)) throw new InvalidOperationException("Header record keys must be strings");
                string name = HeaderByteString(key), value = HeaderByteString(source.Get(key));
                pairs.Add(new[] {name,value});
            }
        }
        foreach (var pair in pairs) {
            if (pair.Length != 2) throw new InvalidOperationException("Header entries require exactly two items");
            result.Append(pair[0],pair[1]);
        }
        return result;
    }
    sealed class HeadersIterator(HeadersValue source, int mode) : TsObject
    {
        int position;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : HeaderIteratorMethods.Prototype;
        internal object Next()
        {
            var rows = source.Rows;
            if (position >= rows.Count) return IteratorResult(Undefined,true);
            var pair = rows[position++];
            return IteratorResult(mode == 1 ? pair.Key : mode == 2 ? pair.Value : new JsArray(new object[] {pair.Key,pair.Value}),false);
        }
    }
    static class HeaderIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var result = new JsClassInstance {ParentPrototype = NativeIteratorPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"Headers Iterator");
            result.DefineMethod("next",new CollectionMethod("next",0,(receiver,_) => receiver is HeadersIterator iterator ? iterator.Next() : throw new InvalidOperationException("Headers iterator receiver required")));
            return result;
        }
    }
    static class HeaderConstructors {internal static readonly JsFunction Value = new HeadersConstructor();}
    sealed class HeadersConstructor : NativeNamespaceFunction
    {
        internal HeadersConstructor() : base("Headers",static _ => throw new InvalidOperationException("Headers constructor requires new"),0)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol),"Headers");
            foreach (bool replace in new[] {false,true}) {
                string method = replace ? "set" : "append";
                prototype.DefineOwn(method,new CollectionMethod(method,2,(receiver,args) => {
                    var target = RequireHeaders(receiver); HeaderArguments(args,2);
                    string name = HeaderByteString(args[0]), value = HeaderByteString(args[1]);
                    target.Append(name,value,replace); return Undefined;
                }));
            }
            foreach (string method in new[] {"get","has","delete"}) {
                prototype.DefineOwn(method,new CollectionMethod(method,1,(receiver,args) => {
                    var target = RequireHeaders(receiver); HeaderArguments(args,1); string name = HeaderName(HeaderByteString(args[0]));
                    if (method == "delete") {target.Remove(name);return Undefined;}
                    bool found = target.Fields.TryGetValue(name,out var values);
                    return method == "has" ? BoxBoolean(found) : found ? HeadersValue.Joined(name,values) : null;
                }));
            }
            prototype.DefineOwn("getSetCookie",new CollectionMethod("getSetCookie",0,(receiver,_) => {
                var target = RequireHeaders(receiver); var result = new JsArray(0);
                if (target.Fields.TryGetValue("set-cookie",out var values)) foreach (string value in values) result.Append(value);
                return result;
            }));
            prototype.DefineOwn("forEach",new CollectionMethod("forEach",1,(receiver,args) => {
                var target = RequireHeaders(receiver); HeaderArguments(args,1);
                if (args[0] is not JsFunction callback) throw new InvalidOperationException("Headers callback must be callable");
                for (int index = 0; index < target.Rows.Count; index++) {
                    var pair = target.Rows[index]; callback.InvokeWithReceiver(FunctionArgument(args,1),new object[] {pair.Value,pair.Key,target});
                }
                return Undefined;
            }));
            foreach (var item in new[] {("entries",0),("keys",1),("values",2)})
                prototype.DefineOwn(item.Item1,new CollectionMethod(item.Item1,0,(receiver,_) => new HeadersIterator(RequireHeaders(receiver),item.Item2)));
            prototype.DefineMethod(PropertyKey(IteratorSymbol),prototype.Get("entries"));
        }
        public override object Construct(object[] args) => CreateHeaders(FunctionArgument(args,0));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var result = CreateHeaders(FunctionArgument(args,0)); result.SetPrototype(ConstructionPrototype(newTarget)); return result;
        }
    }
}
