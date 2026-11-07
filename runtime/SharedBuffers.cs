using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class SharedStorage(byte[] bytes)
    {
        internal readonly byte[] Bytes = bytes;
        internal Dictionary<int, LinkedList<AtomicWaiter>> Waiters;
    }
    sealed class SharedBuffer : BinaryBuffer
    {
        internal readonly SharedStorage Storage;
        internal SharedBuffer(int length) : base(length) { Storage = new(Bytes); }
        internal SharedBuffer(SharedStorage storage) : base(0) { Storage = storage; Bytes = storage.Bytes; }
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
            prototype.DefineClassAccessor("maxByteLength", new CollectionMethod("get maxByteLength", 0, static (receiver, _) => (double)RequireSharedBuffer(receiver).Bytes.Length), 1);
            prototype.DefineClassAccessor("growable", new CollectionMethod("get growable", 0, static (receiver, _) => { RequireSharedBuffer(receiver); return BoxBoolean(false); }), 1);
            prototype.DefineMethod("grow", new CollectionMethod("grow", 1, static (receiver, _) => { RequireSharedBuffer(receiver); throw new InvalidOperationException("SharedArrayBuffer is not growable"); }));
            prototype.DefineMethod("slice", new CollectionMethod("slice", 2, static (receiver, args) => {
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
            if (FunctionArgument(args, 1) is TsObject options && !IsUndefined(options.Get("maxByteLength"))) throw new NotSupportedException("Growable shared buffers are not supported");
            return new SharedBuffer(length);
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget) { var result = (TsObject)Construct(args); result.SetPrototype(ConstructionPrototype(newTarget)); return result; }
    }
    static class SharedBuffers { internal static readonly JsFunction Constructor = new SharedBufferConstructor(); }
}
