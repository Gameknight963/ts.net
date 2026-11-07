using System;
using System.Numerics;

public static partial class Js
{
    sealed class BinaryBuffer(int length) : TsObject
    {
        internal readonly byte[] Bytes = new byte[length];
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)RuntimeNamespaces.ArrayBuffer.Get("prototype");
    }
    sealed class BinaryView(BinaryBuffer buffer, int offset, int length) : TsObject
    {
        internal readonly BinaryBuffer Buffer = buffer;
        internal readonly int Offset = offset, Length = length;
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
    static BinaryBuffer RequireBuffer(object receiver) => receiver as BinaryBuffer ?? throw new InvalidOperationException("ArrayBuffer receiver required");
    static BinaryView RequireView(object receiver) => receiver as BinaryView ?? throw new InvalidOperationException("DataView receiver required");
    sealed class BinaryBufferConstructor : NativeNamespaceFunction
    {
        internal BinaryBufferConstructor() : base("ArrayBuffer", static _ => throw new InvalidOperationException("ArrayBuffer requires new"), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "ArrayBuffer");
            prototype.DefineClassAccessor("byteLength", new CollectionMethod("get byteLength", 0, (receiver, _) => (double)RequireBuffer(receiver).Bytes.Length), 1);
            prototype.DefineMethod("slice", new CollectionMethod("slice", 2, (receiver, args) =>
            {
                var source = RequireBuffer(receiver);
                int size = source.Bytes.Length;
                int start = BufferSliceIndex(FunctionArgument(args, 0), size);
                int end = args.Length < 2 || IsUndefined(args[1]) ? size : BufferSliceIndex(args[1], size);
                var result = new BinaryBuffer(Math.Max(end - start, 0));
                source.Bytes.AsSpan(start, result.Bytes.Length).CopyTo(result.Bytes);
                return result;
            }));
            Member("isView", new JsFunction((FunctionBody)(static args => BoxBoolean(FunctionArgument(args, 0) is BinaryView)), null, "isView", 1));
        }
        public override object Construct(object[] args)
        {
            int length = BinaryIndex(FunctionArgument(args, 0));
            if (FunctionArgument(args, 1) is TsObject options && !IsUndefined(options.Get("maxByteLength")))
                throw new NotSupportedException("Resizable ArrayBuffer is not supported");
            return new BinaryBuffer(length);
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
            prototype.DefineClassAccessor("byteOffset", new CollectionMethod("get byteOffset", 0, (receiver, _) => (double)RequireView(receiver).Offset), 1);
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
            var buffer = RequireBuffer(FunctionArgument(args, 0));
            int offset = BinaryIndex(FunctionArgument(args, 1));
            if (offset > buffer.Bytes.Length) throw new ArgumentOutOfRangeException(null, "DataView offset is out of range");
            int length = IsUndefined(FunctionArgument(args, 2)) ? buffer.Bytes.Length - offset : BinaryIndex(args[2]);
            if (length > buffer.Bytes.Length - offset) throw new ArgumentOutOfRangeException(null, "DataView length is out of range");
            return new BinaryView(buffer, offset, length);
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
    static object AccessView(object receiver, object[] args, string type, bool write)
    {
        var view = RequireView(receiver);
        int offset = BinaryIndex(FunctionArgument(args, 0));
        bool bigint = type.StartsWith("Big", StringComparison.Ordinal);
        BigInteger integer = write && bigint ? BigIntValue(FunctionArgument(args, 1), false) : default;
        double value = write && !bigint ? Number(FunctionArgument(args, 1)) : 0;
        bool little = Truthy(FunctionArgument(args, write ? 2 : 1));
        int size = type.EndsWith("8", StringComparison.Ordinal) ? 1 : type.EndsWith("16", StringComparison.Ordinal) ? 2 : type.EndsWith("32", StringComparison.Ordinal) ? 4 : 8;
        if (offset > view.Length - size) throw new ArgumentOutOfRangeException(null, "DataView access is out of range");
        var bytes = view.Buffer.Bytes.AsSpan(view.Offset + offset, size);
        if (write)
        {
            ulong bits = bigint ? (ulong)(integer & ulong.MaxValue) : type == "Float16" ? BitConverter.HalfToUInt16Bits((Half)value) : type == "Float64" ? unchecked((ulong)BitConverter.DoubleToInt64Bits(value)) : type == "Float32" ? BitConverter.SingleToUInt32Bits((float)value) : unchecked((uint)ToInt32(value));
            for (int i = 0; i < size; i++) bytes[little ? i : size - 1 - i] = (byte)(bits >> (i * 8));
            return Undefined;
        }
        ulong raw = 0;
        for (int i = 0; i < size; i++) raw |= (ulong)bytes[little ? i : size - 1 - i] << (i * 8);
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
