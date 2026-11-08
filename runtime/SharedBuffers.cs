using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class SharedStorage(byte[] bytes, int maximum)
    {
        internal byte[] Bytes = bytes;
        internal readonly int Maximum = maximum;
        internal readonly List<WeakReference<SharedBuffer>> Buffers = maximum >= 0 ? new() : null;
        internal Dictionary<int, LinkedList<AtomicWaiter>> Waiters;
    }
    sealed class SharedBuffer : BinaryBuffer
    {
        internal readonly SharedStorage Storage;
        internal SharedBuffer(int length, int maximum = -1) : base(length, maximum) { Storage = new(Bytes, maximum); if (maximum >= 0) Storage.Buffers.Add(new(this)); }
        internal SharedBuffer(SharedStorage storage) : base(0, storage.Maximum)
        {
            Storage = storage;
            lock (storage) { Bytes = storage.Bytes; if (storage.Maximum >= 0) storage.Buffers.Add(new(this)); }
        }
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)SharedBuffers.Constructor.Get("prototype");
    }
    static SharedBuffer RequireSharedBuffer(object receiver) => receiver as SharedBuffer ?? throw new InvalidOperationException("SharedArrayBuffer receiver required");
    sealed class SharedBufferConstructor : NativeNamespaceFunction
    {
        internal SharedBufferConstructor() : base("SharedArrayBuffer", static _ => throw new InvalidOperationException("SharedArrayBuffer requires new"), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this); prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "SharedArrayBuffer");
            prototype.DefineClassAccessor("byteLength", new CollectionMethod("get byteLength", 0, static (receiver, _) => (double)RequireSharedBuffer(receiver).Bytes.Length), 1);
            prototype.DefineClassAccessor("maxByteLength", new CollectionMethod("get maxByteLength", 0, static (receiver, _) => { var buffer = RequireSharedBuffer(receiver); return (double)(buffer.Resizable ? buffer.Maximum : buffer.Bytes.Length); }), 1);
            prototype.DefineClassAccessor("growable", new CollectionMethod("get growable", 0, static (receiver, _) => BoxBoolean(RequireSharedBuffer(receiver).Resizable)), 1);
            prototype.DefineMethod("grow", new CollectionMethod("grow", 1, static (receiver, args) => GrowSharedBuffer(receiver, FunctionArgument(args, 0))));
            prototype.DefineMethod("slice", new CollectionMethod("slice", 2, static (receiver, args) =>
            {
                var source = RequireSharedBuffer(receiver); int size = source.Bytes.Length;
                int start = BufferSliceIndex(FunctionArgument(args, 0), size);
                int end = IsUndefined(FunctionArgument(args, 1)) ? size : BufferSliceIndex(args[1], size);
                int length = Math.Max(end - start, 0); var result = BufferSpeciesCreate(source, length);
                source.Bytes.AsSpan(start, length).CopyTo(result.Bytes); return result;
            }));
            string speciesKey = PropertyKey(SpeciesSymbol); var speciesGetter = new ConstructorSpeciesGetter();
            Member(speciesKey, Undefined); DefineAccessor(speciesKey, speciesGetter, 1); speciesGetter.InferPropertyName("get [Symbol.species]");
        }
        public override object Construct(object[] args)
        {
            int length = BinaryIndex(FunctionArgument(args, 0));
            object maximum = FunctionArgument(args, 1) is TsObject options ? options.Get("maxByteLength") : Undefined;
            int limit = IsUndefined(maximum) ? -1 : BinaryIndex(maximum);
            if (limit >= 0 && length > limit) throw new ArgumentOutOfRangeException(null, "SharedArrayBuffer exceeds maximum length");
            return new SharedBuffer(length, limit);
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) { var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result; }
    }
    static object GrowSharedBuffer(object receiver, object value)
    {
        var buffer = RequireSharedBuffer(receiver);
        if (!buffer.Resizable) throw new InvalidOperationException("SharedArrayBuffer is not growable");
        int length = BinaryIndex(value);
        var storage = buffer.Storage;
        lock (storage)
        {
            if (length < storage.Bytes.Length || length > storage.Maximum) throw new ArgumentOutOfRangeException(null, "SharedArrayBuffer growth is out of range");
            if (length == storage.Bytes.Length) return Undefined;
            Array.Resize(ref storage.Bytes, length);
            for (int index = storage.Buffers.Count - 1; index >= 0; index--)
                if (storage.Buffers[index].TryGetTarget(out var alias)) System.Threading.Volatile.Write(ref alias.Bytes, storage.Bytes);
                else storage.Buffers.RemoveAt(index);
        }
        return Undefined;
    }
    static class SharedBuffers { internal static readonly JsFunction Constructor = new SharedBufferConstructor(); }
}
