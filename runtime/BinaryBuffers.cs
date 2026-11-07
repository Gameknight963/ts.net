using System;
using System.Numerics;
using System.Buffers.Binary;

public static partial class Js
{
    class BinaryBuffer(int length, int maximum = -1) : TsObject
    {
        internal byte[] Bytes = length == 0 ? Array.Empty<byte>() : new byte[length];
        internal bool Detached;
        internal void Detach() { Bytes = Array.Empty<byte>(); Detached = true; }
        internal readonly int Maximum = maximum;
        internal bool Resizable => Maximum >= 0;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)RuntimeNamespaces.ArrayBuffer.Get("prototype");
    }
    sealed class BinaryView(BinaryBuffer buffer, int offset, int length, bool tracking = false) : TsObject
    {
        internal readonly BinaryBuffer Buffer = buffer;
        internal readonly int Offset = offset, FixedLength = length;
        internal readonly bool Tracking = tracking;
        internal bool OutOfBounds => Buffer.Detached || Offset > Buffer.Bytes.Length || !Tracking && FixedLength > Buffer.Bytes.Length - Offset;
        internal int Length => OutOfBounds ? throw new InvalidOperationException("DataView is out of bounds") : Tracking ? Buffer.Bytes.Length - Offset : FixedLength;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)RuntimeNamespaces.DataView.Get("prototype");
    }
    static int BinaryIndex(object value)
    {
        double number = Number(value);
        if (double.IsNaN(number)) return 0;
        number = Math.Truncate(number);
        if (number < 0 || number > Array.MaxLength) throw new ArgumentOutOfRangeException(null, "Binary buffer index is out of range");
        return (int)number;
    }
    static BinaryBuffer RequireBufferSource(object receiver) => receiver as BinaryBuffer ?? throw new InvalidOperationException("Buffer receiver required");
    static BinaryBuffer RequireBuffer(object receiver) => receiver is BinaryBuffer value && value is not SharedBuffer ? value : throw new InvalidOperationException("ArrayBuffer receiver required");
    static BinaryView RequireView(object receiver) => receiver as BinaryView ?? throw new InvalidOperationException("DataView receiver required");
    sealed class BinaryBufferConstructor : NativeNamespaceFunction
    {
        internal BinaryBufferConstructor() : base("ArrayBuffer", static _ => throw new InvalidOperationException("ArrayBuffer requires new"), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "ArrayBuffer");
            prototype.DefineClassAccessor("byteLength", new CollectionMethod("get byteLength", 0, (receiver, _) => (double)RequireBuffer(receiver).Bytes.Length), 1);
            prototype.DefineClassAccessor("resizable", new CollectionMethod("get resizable", 0, (receiver, _) => BoxBoolean(RequireBuffer(receiver).Resizable)), 1);
            prototype.DefineClassAccessor("maxByteLength", new CollectionMethod("get maxByteLength", 0, (receiver, _) => { var buffer = RequireBuffer(receiver); return (double)(buffer.Detached ? 0 : buffer.Resizable ? buffer.Maximum : buffer.Bytes.Length); }), 1);
            prototype.DefineClassAccessor("detached", new CollectionMethod("get detached", 0, (receiver, _) => BoxBoolean(RequireBuffer(receiver).Detached)), 1);
            foreach (bool fixedLength in new[] { false, true }) {
                string method = fixedLength ? "transferToFixedLength" : "transfer";
                prototype.DefineMethod(method, new CollectionMethod(method, 0, (receiver, args) => TransferBuffer(receiver, FunctionArgument(args, 0), fixedLength)));
            }
            prototype.DefineMethod("resize", new CollectionMethod("resize", 1, (receiver, args) => {
                var buffer = RequireBuffer(receiver);
                if (!buffer.Resizable) throw new InvalidOperationException("ArrayBuffer is not resizable");
                int length = BinaryIndex(FunctionArgument(args, 0));
                EnsureAttached(buffer);
                if (length > buffer.Maximum) throw new ArgumentOutOfRangeException(null, "ArrayBuffer exceeds maximum length");
                Array.Resize(ref buffer.Bytes, length);
                return Undefined;
            }));
            prototype.DefineMethod("slice", new CollectionMethod("slice", 2, (receiver, args) =>
            {
                var source = RequireBuffer(receiver);
                EnsureAttached(source);
                int size = source.Bytes.Length;
                int start = BufferSliceIndex(FunctionArgument(args, 0), size);
                int end = args.Length < 2 || IsUndefined(args[1]) ? size : BufferSliceIndex(args[1], size);
                int length = Math.Max(end - start, 0);
                var result = BufferSpeciesCreate(source, length);
                EnsureAttached(source);
                int available = Math.Min(length, Math.Max(source.Bytes.Length - start, 0));
                if (available != 0) source.Bytes.AsSpan(start, available).CopyTo(result.Bytes);
                return result;
            }));
            string speciesKey = PropertyKey(SpeciesSymbol); var speciesGetter = new ConstructorSpeciesGetter();
            Member(speciesKey, Undefined); DefineAccessor(speciesKey, speciesGetter, 1); speciesGetter.InferPropertyName("get [Symbol.species]");
            Member("isView", new JsFunction((FunctionBody)(static args => BoxBoolean(FunctionArgument(args, 0) is BinaryView or TypedArrayValue)), null, "isView", 1));
        }
        public override object Construct(object[] args)
        {
            int length = BinaryIndex(FunctionArgument(args, 0));
            object requested = FunctionArgument(args, 1) is TsObject options ? options.Get("maxByteLength") : Undefined;
            int maximum = IsUndefined(requested) ? -1 : BinaryIndex(requested);
            if (maximum >= 0 && length > maximum) throw new ArgumentOutOfRangeException(null, "Initial buffer exceeds maximum length");
            return new BinaryBuffer(length, maximum);
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
    static int BufferSliceIndex(object value, int length)
    {
        double index = Number(value);
        if (double.IsNaN(index)) return 0;
        index = Math.Truncate(index);
        return (int)(index < 0 ? Math.Max(length + index, 0) : Math.Min(index, length));
    }
    sealed class BinaryViewConstructor : NativeNamespaceFunction
    {
        internal BinaryViewConstructor() : base("DataView", static _ => throw new InvalidOperationException("DataView requires new"), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "DataView");
            prototype.DefineClassAccessor("buffer", new CollectionMethod("get buffer", 0, (receiver, _) => RequireView(receiver).Buffer), 1);
            prototype.DefineClassAccessor("byteOffset", new CollectionMethod("get byteOffset", 0, (receiver, _) => (double)ViewOffset(RequireView(receiver))), 1);
            prototype.DefineClassAccessor("byteLength", new CollectionMethod("get byteLength", 0, (receiver, _) => (double)RequireView(receiver).Length), 1);
            foreach (string type in new[] { "Int8", "Uint8", "Int16", "Uint16", "Int32", "Uint32", "Float16", "Float32", "Float64", "BigInt64", "BigUint64" })
            {
                string captured = type;
                prototype.DefineMethod("get" + type, new CollectionMethod("get" + type, 1, (receiver, args) => AccessView(receiver, args, captured, false)));
                prototype.DefineMethod("set" + type, new CollectionMethod("set" + type, 2, (receiver, args) => AccessView(receiver, args, captured, true)));
            }
        }
        public override object Construct(object[] args)
        {
            var buffer = RequireBufferSource(FunctionArgument(args, 0));
            int offset = BinaryIndex(FunctionArgument(args, 1));
            EnsureAttached(buffer);
            if (offset > buffer.Bytes.Length) throw new ArgumentOutOfRangeException(null, "DataView offset is out of range");
            int length = IsUndefined(FunctionArgument(args, 2)) ? buffer.Bytes.Length - offset : BinaryIndex(args[2]);
            EnsureAttached(buffer);
            if (length > buffer.Bytes.Length - offset) throw new ArgumentOutOfRangeException(null, "DataView length is out of range");
            return new BinaryView(buffer, offset, length, buffer.Resizable && IsUndefined(FunctionArgument(args, 2)));
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
    static int ViewOffset(BinaryView view)
    {
        _ = view.Length;
        return view.Offset;
    }
    static object AccessView(object receiver, object[] args, string type, bool write)
    {
        var view = RequireView(receiver);
        int offset = BinaryIndex(FunctionArgument(args, 0));
        bool bigint = type.StartsWith("Big", StringComparison.Ordinal);
        BigInteger integer = write && bigint ? BigIntValue(FunctionArgument(args, 1), false) : default;
        double value = write && !bigint ? Number(FunctionArgument(args, 1)) : 0;
        bool little = Truthy(FunctionArgument(args, write ? 2 : 1));
        int size = BinarySize(type);
        if (offset > view.Length - size) throw new ArgumentOutOfRangeException(null, "DataView access is out of range");
        var bytes = view.Buffer.Bytes.AsSpan(view.Offset + offset, size);
        if (write) { WriteBinary(bytes, type, little, value, integer); return Undefined; }
        return ReadBinary(bytes, type, little);
    }
    static int BinarySize(string type) => type.EndsWith("8", StringComparison.Ordinal) ? 1 : type.EndsWith("16", StringComparison.Ordinal) ? 2 : type.EndsWith("32", StringComparison.Ordinal) ? 4 : 8;
    static void WriteBinary(Span<byte> bytes, string type, bool little, double value, BigInteger integer)
    {
        ulong bits = type.StartsWith("Big", StringComparison.Ordinal) ? (ulong)(integer & ulong.MaxValue) : type == "Float16" ? BitConverter.HalfToUInt16Bits((Half)value) : type == "Float64" ? unchecked((ulong)BitConverter.DoubleToInt64Bits(value)) : type == "Float32" ? BitConverter.SingleToUInt32Bits((float)value) : unchecked((uint)ToInt32(value));
        if (bytes.Length == 1) bytes[0] = (byte)bits;
        else if (bytes.Length == 2) { if (little) BinaryPrimitives.WriteUInt16LittleEndian(bytes, (ushort)bits); else BinaryPrimitives.WriteUInt16BigEndian(bytes, (ushort)bits); }
        else if (bytes.Length == 4) { if (little) BinaryPrimitives.WriteUInt32LittleEndian(bytes, (uint)bits); else BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)bits); }
        else { if (little) BinaryPrimitives.WriteUInt64LittleEndian(bytes, bits); else BinaryPrimitives.WriteUInt64BigEndian(bytes, bits); }
    }
    static object ReadBinary(ReadOnlySpan<byte> bytes, string type, bool little)
    {
        int size = bytes.Length;
        bool bigint = type.StartsWith("Big", StringComparison.Ordinal);
        ulong raw = size switch
        {
            1 => bytes[0],
            2 => little ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) : BinaryPrimitives.ReadUInt16BigEndian(bytes),
            4 => little ? BinaryPrimitives.ReadUInt32LittleEndian(bytes) : BinaryPrimitives.ReadUInt32BigEndian(bytes),
            _ => little ? BinaryPrimitives.ReadUInt64LittleEndian(bytes) : BinaryPrimitives.ReadUInt64BigEndian(bytes)
        };
        if (bigint) return type == "BigInt64" ? new BigInteger(unchecked((long)raw)) : new BigInteger(raw);
        return type switch
        {
            "Float16" => (double)BitConverter.UInt16BitsToHalf((ushort)raw),
            "Int8" => (double)unchecked((sbyte)raw),
            "Uint8" => (double)raw,
            "Int16" => (double)unchecked((short)raw),
            "Uint16" => (double)raw,
            "Int32" => (double)unchecked((int)raw),
            "Uint32" => (double)raw,
            "Float32" => (double)BitConverter.UInt32BitsToSingle((uint)raw),
            _ => BitConverter.Int64BitsToDouble(unchecked((long)raw))
        };
    }
}
