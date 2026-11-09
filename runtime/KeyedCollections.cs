using System.Numerics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal sealed class CollectionKeyComparer : IEqualityComparer<object>, IAlternateEqualityComparer<double, object>
{
    internal static readonly CollectionKeyComparer Instance = new();
    public new bool Equals(object first, object second) => ReferenceEquals(first, second) || ((first, second) switch
    {
        (double a, double b) => a == b || double.IsNaN(a) && double.IsNaN(b),
        (string a, string b) => StringComparer.Ordinal.Equals(a, b),
        (bool a, bool b) => a == b,
        (BigInteger a, BigInteger b) => a == b,
        _ => false
    });
    public int GetHashCode(object value) => value switch
    {
        double number => number.GetHashCode(),
        string text => StringComparer.Ordinal.GetHashCode(text),
        bool boolean => boolean.GetHashCode(),
        BigInteger integer => integer.GetHashCode(),
        _ => RuntimeHelpers.GetHashCode(value)
    };
    public bool Equals(double first, object second) => second is double number && (first == number || double.IsNaN(first) && double.IsNaN(number));
    public int GetHashCode(double value) => value.GetHashCode();
    public object Create(double value) => value;
}

internal sealed class CollectionEntry(object key, object value)
{
    internal object Key = key, Value = value;
    internal bool Active = true;
    internal void Remove() { Active = false; Key = Value = Js.Undefined; }
}

public sealed class JsKeyedCollection : JsDescriptorObject
{
    static readonly object NullKey = new();
    internal readonly bool IsSet;
    internal List<CollectionEntry> Entries = new();
    Dictionary<object, int> index = new(CollectionKeyComparer.Instance);
    readonly List<WeakReference<JsCollectionIterator>> iterators = new();
    int pruneAt = 64;
    internal JsKeyedCollection(bool set) { IsSet = set; }
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.KeyedCollectionPrototype(IsSet);
    internal int Size => index.Count;
    static object Key(object value) => value ?? NullKey;
    internal bool Contains(object key) => index.ContainsKey(Key(key));
    internal object Read(object key) => index.TryGetValue(Key(key), out int slot) ? Entries[slot].Value : Js.Undefined;
    internal object ReadNumber(double key) => index.GetAlternateLookup<double>().TryGetValue(key, out int slot) ? Entries[slot].Value : Js.Undefined;
    internal void Write(object key, object value)
    {
        if (key is double number && number == 0) key = 0d;
        object lookup = Key(key);
        if (index.TryGetValue(lookup, out int slot)) { if (!IsSet) Entries[slot].Value = value; return; }
        index.Add(lookup, Entries.Count);
        Entries.Add(new CollectionEntry(key, IsSet ? key : value));
    }
    internal bool Remove(object key)
    {
        if (!index.Remove(Key(key), out int slot)) return false;
        Entries[slot].Remove(); Compact(); return true;
    }
    internal void ClearEntries()
    {
        foreach (var entry in Entries) entry.Remove();
        index.Clear(); Compact();
    }
    internal void Register(JsCollectionIterator iterator)
    {
        if (iterators.Count >= pruneAt)
        {
            iterators.RemoveAll(reference => !reference.TryGetTarget(out var cursor) || cursor.Owner != this);
            pruneAt = Math.Max(64, iterators.Count * 2);
        }
        iterators.Add(new(iterator));
    }
    void Compact()
    {
        if (Entries.Count <= (long)Size * 2 + 64) return;
        var positions = new int[Entries.Count + 1];
        var live = new List<CollectionEntry>(Size);
        var lookup = new Dictionary<object, int>(Size, CollectionKeyComparer.Instance);
        for (int i = 0; i < Entries.Count; i++)
        {
            positions[i] = live.Count;
            var entry = Entries[i];
            if (!entry.Active) continue;
            lookup.Add(Key(entry.Key), live.Count); live.Add(entry);
        }
        positions[Entries.Count] = live.Count;
        iterators.RemoveAll(reference =>
        {
            if (!reference.TryGetTarget(out var cursor) || cursor.Owner != this) return true;
            cursor.Position = positions[cursor.Position]; return false;
        });
        Entries = live; index = lookup;
    }
}

public sealed class JsCollectionIterator : JsDescriptorObject
{
    internal JsKeyedCollection Owner;
    internal int Position;
    readonly int mode; // 0: keys, 1: values, 2: entries
    readonly bool set;
    internal bool IsSetIterator => set;
    bool cached;
    object key, value;
    internal JsCollectionIterator(JsKeyedCollection owner, int mode)
    {
        Owner = owner; this.mode = mode; set = owner.IsSet; owner.Register(this);
    }
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.KeyedIteratorPrototype(set);
    internal bool Ensure()
    {
        if (cached) return true;
        if (Owner == null) return false;
        while (Position < Owner.Entries.Count)
        {
            var entry = Owner.Entries[Position++];
            if (!entry.Active) continue;
            key = entry.Key; value = entry.Value; cached = true; return true;
        }
        Finish(); return false;
    }
    internal bool Take(out object first, out object second)
    {
        if (!Ensure()) { first = second = Js.Undefined; return false; }
        first = key; second = value; key = value = null; cached = false; return true;
    }
    internal object TakeValue()
    {
        if (!Take(out object first, out object second)) return Js.Undefined;
        return mode == 0 ? first : mode == 1 ? second : new JsArray(new[] { first, set ? first : second });
    }
    internal void Finish() { Owner = null; cached = false; key = value = null; }
    internal object Next()
    {
        bool more = Ensure();
        var result = new JsDynamicObject();
        result.DefineOwn("value", more ? TakeValue() : Js.Undefined);
        result.DefineOwn("done", Js.BoxBoolean(!more)); return result;
    }
}

public static partial class Js
{
    sealed class MapGetFunction() : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Map.get requires a receiver")), null, "get", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => CollectionReceiver(receiver, false).Read(FunctionArgument(arguments, 0));
        public override double? InvokeMethodNumber1(object receiver, double? first)
        {
            var map = CollectionReceiver(receiver, false);
            return Cast<double?>(first.HasValue ? map.ReadNumber(first.Value) : map.Read(Undefined), "Collection result violates number type");
        }
    }
    sealed class CollectionMethod(string name, int length, Func<object, object[], object> action)
        : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Collection method requires a receiver")), null, name, length)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => action(receiver, arguments);
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(action(receiver, Array.Empty<object>()), "Collection result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => Cast<double?>(action(receiver, new[] { BoxNumber(first) }), "Collection result violates number type");
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Cast<double?>(action(receiver, new[] { BoxNumber(first), BoxNumber(second) }), "Collection result violates number type");
    }
    static JsKeyedCollection CollectionReceiver(object receiver, bool set) => receiver is JsKeyedCollection collection && collection.IsSet == set ? collection : throw new InvalidOperationException("Incompatible collection receiver");
    sealed class CollectionConstructor : NativeNamespaceFunction
    {
        readonly bool set;
        internal CollectionConstructor(bool set) : base(set ? "Set" : "Map", static _ => throw new InvalidOperationException("Collection constructor requires new"), 0)
        {
            this.set = set;
            string speciesKey = PropertyKey(SpeciesSymbol);
            var speciesGetter = new ConstructorSpeciesGetter();
            Member(speciesKey, Undefined); DefineAccessor(speciesKey, speciesGetter, 1);
            speciesGetter.InferPropertyName("get [Symbol.species]");
            var prototype = new JsCreatedObject();
            prototype.SetPrototype(DefaultClassObjectPrototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), set ? "Set" : "Map", false);
            CollectionMethod Method(string name, int length, Func<JsKeyedCollection, object[], object> action) =>
                new(name, length, (receiver, arguments) => action(CollectionReceiver(receiver, set), arguments));
            prototype.Define(new("size", Undefined, Method("get size", 0, (collection, _) => (double)collection.Size), null, true, false, false, true));
            prototype.DefineMethod("has", Method("has", 1, (collection, args) => BoxBoolean(collection.Contains(FunctionArgument(args, 0)))));
            prototype.DefineMethod("delete", Method("delete", 1, (collection, args) => BoxBoolean(collection.Remove(FunctionArgument(args, 0)))));
            prototype.DefineMethod("clear", Method("clear", 0, (collection, _) => { collection.ClearEntries(); return Undefined; }));
            prototype.DefineMethod(set ? "add" : "set", Method(set ? "add" : "set", set ? 1 : 2, (collection, args) => { collection.Write(FunctionArgument(args, 0), FunctionArgument(args, 1)); return collection; }));
            if (!set) prototype.DefineMethod("get", new MapGetFunction());
            if (!set) foreach (bool computed in new[] {false,true}) {
                string name = computed ? "getOrInsertComputed" : "getOrInsert";
                prototype.DefineMethod(name,Method(name,2,(collection,args) => {
                    object key = FunctionArgument(args,0);
                    JsFunction callback = computed ? FunctionArgument(args,1) as JsFunction ?? throw new InvalidOperationException("Map callback must be callable") : null;
                    if (key is double number && number == 0) key = 0d;
                    if (collection.Contains(key)) return collection.Read(key);
                    object value = computed ? callback.InvokeWithReceiver(Undefined,new[] {key}) : FunctionArgument(args,1);
                    collection.Write(key,value); return value;
                }));
            }
            var values = Method("values", 0, (collection, _) => new JsCollectionIterator(collection, 1));
            prototype.DefineMethod("values", values);
            prototype.DefineMethod("keys", set ? values : Method("keys", 0, (collection, _) => new JsCollectionIterator(collection, 0)));
            var entries = Method("entries", 0, (collection, _) => new JsCollectionIterator(collection, 2));
            prototype.DefineMethod("entries", entries);
            prototype.DefineMethod(PropertyKey(IteratorSymbol), set ? values : entries);
            prototype.DefineMethod("forEach", Method("forEach", 1, (collection, args) =>
            {
                object callback = FunctionArgument(args, 0), receiver = FunctionArgument(args, 1);
                if (callback is not JsFunction) throw new InvalidOperationException("Collection callback is not callable");
                var cursor = new JsCollectionIterator(collection, 2);
                try { while (cursor.Take(out object key, out object value)) CallMethod(callback, receiver, new[] { value, key, collection }); }
                finally { cursor.Finish(); }
                return Undefined;
            }));
            if (set)
            {
                prototype.DefineMethod("symmetricDifference", Method("symmetricDifference", 1, (collection, args) => SetSymmetricDifference(collection, FunctionArgument(args, 0))));
                prototype.DefineMethod("isSubsetOf", Method("isSubsetOf", 1, (collection, args) => TestSetRelation(collection, FunctionArgument(args, 0), SetRelation.Subset)));
                prototype.DefineMethod("isSupersetOf", Method("isSupersetOf", 1, (collection, args) => TestSetRelation(collection, FunctionArgument(args, 0), SetRelation.Superset)));
                prototype.DefineMethod("isDisjointFrom", Method("isDisjointFrom", 1, (collection, args) => TestSetRelation(collection, FunctionArgument(args, 0), SetRelation.Disjoint)));
            }
            if (set) prototype.DefineMethod("intersection", Method("intersection", 1, (collection, args) => SetIntersection(collection, FunctionArgument(args, 0))));
            if (set) prototype.DefineMethod("difference", Method("difference", 1, (collection, args) => SetDifference(collection, FunctionArgument(args, 0))));
            if (set) prototype.DefineMethod("union", Method("union", 1, (collection, args) => SetUnion(collection, FunctionArgument(args, 0))));
            if (!set) Member("groupBy", new JsFunction((FunctionBody)(args => MapGroupBy(FunctionArgument(args, 0), FunctionArgument(args, 1))), null, "groupBy", 2));
            UseObjectPrototype(prototype);
        }
        object Create(object[] arguments, TsObject prototype)
        {
            var result = new JsKeyedCollection(set); if (prototype != null) result.SetPrototype(prototype);
            object input = FunctionArgument(arguments, 0);
            if (IsNullish(input)) return result;
            object adder = result.Get(set ? "add" : "set");
            if (adder is not JsFunction) throw new InvalidOperationException("Collection adder is not callable");
            object source = Iteration.Start(input);
            int position = 0; bool closeable = false;
            try
            {
                while (true)
                {
                    closeable = false;
                    if (position >= Iteration.Length(source)) break;
                    object value = Iteration.Value(source, position);
                    position += Iteration.Step(source, position); closeable = true;
                    if (set) CallMethod(adder, result, new[] { value });
                    else
                    {
                        if (value is not TsObject) throw new InvalidOperationException("Map entry is not an object");
                        object key = Js.Get(value, "0"), item = Js.Get(value, "1");
                        CallMethod(adder, result, new[] { key, item });
                    }
                }
            }
            catch { if (closeable) CloseIterationAt(source, position, true); throw; }
            return result;
        }
        public override object Construct(object[] arguments) => Create(arguments, null);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments) => Create(arguments, ((TsObject)instance).PropertyPrototype);
    }
    static class CollectionIteratorPrototypes
    {
        internal static readonly JsFunction MapNext = Next(false), SetNext = Next(true);
        internal static readonly TsObject Map = Create(false), Set = Create(true);
        static JsFunction Next(bool set) => new CollectionMethod("next", 0, (receiver, _) => receiver is JsCollectionIterator iterator && iterator.IsSetIterator == set ? iterator.Next() : throw new InvalidOperationException("Incompatible iterator receiver"));
        static TsObject Create(bool set)
        {
            var prototype = new JsCreatedObject();
            prototype.SetPrototype(NativeIteratorPrototype);
            prototype.DefineMethod("next", set ? SetNext : MapNext);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), set ? "Set Iterator" : "Map Iterator", false);
            return prototype;
        }
    }
    internal static TsObject KeyedIteratorPrototype(bool set) => set ? CollectionIteratorPrototypes.Set : CollectionIteratorPrototypes.Map;
    internal static JsFunction KeyedIteratorIntrinsicNext(bool set) => set ? CollectionIteratorPrototypes.SetNext : CollectionIteratorPrototypes.MapNext;
    internal static TsObject KeyedCollectionPrototype(bool set) => (TsObject)(set ? RuntimeNamespaces.Set : RuntimeNamespaces.Map).Get("prototype");
}
