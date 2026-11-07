using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

public static partial class Js
{
    sealed class SearchParamsValue : TsObject
    {
        internal readonly List<KeyValuePair<string, string>> Pairs = new();
        internal Action Changed;
        internal void Notify() => Changed?.Invoke();
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RuntimeNamespaces.URLSearchParams.Get("prototype");
        internal string Serialize()
        {
            var result = new StringBuilder();
            foreach (var pair in Pairs)
            {
                if (result.Length != 0) result.Append('&');
                result.Append(FormEncode(pair.Key)).Append('=').Append(FormEncode(pair.Value));
            }
            return result.ToString();
        }
        internal void SetPair(string key, string value)
        {
            bool found = false; int write = 0;
            for (int read = 0; read < Pairs.Count; read++)
            {
                var pair = Pairs[read];
                if (pair.Key == key)
                {
                    if (found) continue;
                    found = true; pair = new(key, value);
                }
                Pairs[write++] = pair;
            }
            if (write < Pairs.Count) Pairs.RemoveRange(write, Pairs.Count - write);
            if (!found) Pairs.Add(new(key, value));
        }
    }
    sealed class SearchParamsIterator(SearchParamsValue owner, int mode) : TsObject
    {
        readonly SearchParamsValue source = owner;
        int position;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : SearchParamsIteratorMethods.Prototype;
        internal object Next()
        {
            if (position >= source.Pairs.Count) return IteratorResult(Undefined, true);
            var pair = source.Pairs[position++];
            return IteratorResult(mode == 1 ? pair.Key : mode == 2 ? pair.Value : new JsArray(new object[] { pair.Key, pair.Value }), false);
        }
    }
    static class SearchParamsIteratorMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var prototype = new JsClassInstance { ParentPrototype = NativeIteratorPrototype };
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "URLSearchParams Iterator");
            prototype.DefineMethod("next", new CollectionMethod("next", 0, (receiver, _) => receiver is SearchParamsIterator iterator ? iterator.Next() : throw new InvalidOperationException("URLSearchParams iterator receiver required")));
            return prototype;
        }
    }
    static string QueryText(object value)
    {
        string text = StringValue(value);
        return FirstUnpairedSurrogate(text) < 0 ? text : Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text));
    }
    static string QueryArgument(object[] arguments, int index)
    {
        if (index >= arguments.Length) throw new InvalidOperationException("Missing URLSearchParams argument");
        return QueryText(arguments[index]);
    }
    static string FormEncode(string value) => WebUtility.UrlEncode(value).Replace("!", "%21").Replace("(", "%28").Replace(")", "%29").Replace("~", "%7E");
    static SearchParamsValue RequireSearchParams(object value) => value as SearchParamsValue ?? throw new InvalidOperationException("URLSearchParams receiver required");
    static SearchParamsValue CreateSearchParams(object input)
    {
        var result = new SearchParamsValue();
        if (IsNullish(input)) return result;
        if (input is TsObject source)
        {
            bool custom = TryStartCustomIterable(input, out var cursor);
            if (custom || input is JsArray or JsArguments or JsKeyedCollection or JsCollectionIterator or JsSequenceIterator || IsIteratorObject(input))
            {
                cursor = custom ? cursor : Iteration.Start(input);
                int position = 0;
                try
                {
                    while (position < Iteration.Length(cursor))
                    {
                        object pair = Iteration.Value(cursor, position);
                        position += Iteration.Step(cursor, position);
                        if (pair is not TsObject) throw new InvalidOperationException("Query entries must be iterable pairs");
                        object values = Iteration.Start(pair); int index = 0;
                        try
                        {
                            if (index >= Iteration.Length(values)) throw new InvalidOperationException("Query entries must contain two items");
                            string key = QueryText(Iteration.Value(values, index)); index += Iteration.Step(values, index);
                            if (index >= Iteration.Length(values)) throw new InvalidOperationException("Query entries must contain two items");
                            string value = QueryText(Iteration.Value(values, index)); index += Iteration.Step(values, index);
                            if (index < Iteration.Length(values)) throw new InvalidOperationException("Query entries must contain two items");
                            result.Pairs.Add(new(key, value));
                        }
                        catch { CloseIterationAt(values, index, true); throw; }
                    }
                }
                catch { CloseIterationAt(cursor, position, true); throw; }
                return result;
            }
            var keys = (JsArray)source.OwnKeys();
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            for (uint index = 0; index < keys.Length(); index++)
            {
                string key = (string)keys.ElementAt(index);
                if (!source.HasOwn(key) || !source.ForInEnumerable(key)) continue;
                if (IsSymbolKey(key)) throw new InvalidOperationException("Query record keys must be strings");
                string name = QueryText(key), value = QueryText(source.Get(key));
                if (positions.TryGetValue(name, out int prior)) result.Pairs[prior] = new(name, value);
                else { positions.Add(name, result.Pairs.Count); result.Pairs.Add(new(name, value)); }
            }
            return result;
        }
        string text = QueryText(input);
        if (text.StartsWith('?')) text = text[1..];
        // Malformed UTF-8 follows the CLR decoder's replacement behavior.
        foreach (string part in text.Split('&'))
        {
            if (part.Length == 0) continue;
            int equal = part.IndexOf('=');
            string key = equal < 0 ? part : part[..equal], value = equal < 0 ? "" : part[(equal + 1)..];
            result.Pairs.Add(new(WebUtility.UrlDecode(key), WebUtility.UrlDecode(value)));
        }
        return result;
    }
    sealed class SearchParamsConstructor : NativeNamespaceFunction
    {
        internal SearchParamsConstructor() : base("URLSearchParams", static _ => throw new InvalidOperationException("URLSearchParams requires new"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "URLSearchParams");
            prototype.DefineClassAccessor("size", new CollectionMethod("get size", 0, (receiver, _) => (double)RequireSearchParams(receiver).Pairs.Count), 1);
            prototype.DefineMethod("append", new CollectionMethod("append", 2, (receiver, args) => { var target = RequireSearchParams(receiver); string key = QueryArgument(args, 0), value = QueryArgument(args, 1); target.Pairs.Add(new(key, value)); target.Notify(); return Undefined; }));
            prototype.DefineMethod("set", new CollectionMethod("set", 2, (receiver, args) => { var target = RequireSearchParams(receiver); string key = QueryArgument(args, 0), value = QueryArgument(args, 1); target.SetPair(key, value); target.Notify(); return Undefined; }));
            prototype.DefineMethod("delete", new CollectionMethod("delete", 1, (receiver, args) =>
            {
                var target = RequireSearchParams(receiver); string key = QueryArgument(args, 0);
                bool filter = !IsUndefined(FunctionArgument(args, 1)); string value = filter ? QueryText(args[1]) : null;
                target.Pairs.RemoveAll(pair => pair.Key == key && (!filter || pair.Value == value)); target.Notify(); return Undefined;
            }));
            prototype.DefineMethod("has", new CollectionMethod("has", 1, (receiver, args) =>
            {
                var target = RequireSearchParams(receiver); string key = QueryArgument(args, 0);
                bool filter = !IsUndefined(FunctionArgument(args, 1)); string value = filter ? QueryText(args[1]) : null;
                return BoxBoolean(target.Pairs.Exists(pair => pair.Key == key && (!filter || pair.Value == value)));
            }));
            prototype.DefineMethod("get", new CollectionMethod("get", 1, (receiver, args) =>
            {
                var target = RequireSearchParams(receiver); string key = QueryArgument(args, 0);
                foreach (var pair in target.Pairs) if (pair.Key == key) return pair.Value;
                return null;
            }));
            prototype.DefineMethod("getAll", new CollectionMethod("getAll", 1, (receiver, args) =>
            {
                var target = RequireSearchParams(receiver); string key = QueryArgument(args, 0); var result = new JsArray(0);
                foreach (var pair in target.Pairs) if (pair.Key == key) result.Append(pair.Value);
                return result;
            }));
            prototype.DefineMethod("sort", new CollectionMethod("sort", 0, (receiver, _) =>
            {
                var target = RequireSearchParams(receiver);
                var sorted = target.Pairs.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
                target.Pairs.Clear(); target.Pairs.AddRange(sorted); target.Notify(); return Undefined;
            }));
            prototype.DefineMethod("forEach", new CollectionMethod("forEach", 1, (receiver, args) =>
            {
                var target = RequireSearchParams(receiver);
                if (FunctionArgument(args, 0) is not JsFunction callback) throw new InvalidOperationException("Query callback must be callable");
                for (int index = 0; index < target.Pairs.Count; index++)
                {
                    var pair = target.Pairs[index]; callback.InvokeWithReceiver(FunctionArgument(args, 1), new object[] { pair.Value, pair.Key, target });
                }
                return Undefined;
            }));
            foreach (var item in new[] { ("entries", 0), ("keys", 1), ("values", 2) })
                prototype.DefineMethod(item.Item1, new CollectionMethod(item.Item1, 0, (receiver, _) => new SearchParamsIterator(RequireSearchParams(receiver), item.Item2)));
            prototype.DefineMethod(PropertyKey(IteratorSymbol), prototype.Get("entries"));
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, (receiver, _) => RequireSearchParams(receiver).Serialize()));
        }
        public override object Construct(object[] args) => CreateSearchParams(FunctionArgument(args, 0));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
