using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;

public static partial class Js
{
    static readonly string[] TypedArrayNames = { "Int8Array", "Uint8Array", "Uint8ClampedArray", "Int16Array", "Uint16Array", "Int32Array", "Uint32Array", "Float16Array", "Float32Array", "Float64Array", "BigInt64Array", "BigUint64Array" };
    static readonly Dictionary<string, JsFunction> TypedArrayConstructors = new();
    static JsFunction GetTypedArrayConstructor(string name)
    {
        if (!TypedArrayConstructors.TryGetValue(name, out var result)) TypedArrayConstructors.Add(name, result = new TypedArrayConstructor(name));
        return result;
    }
    sealed class TypedArrayValue : TsObject
    {
        internal readonly BinaryBuffer Buffer;
        TsObject prototype;
        internal readonly int Offset, FixedCount, Size;
        internal readonly bool Tracking;
        internal bool OutOfBounds => Buffer.Detached || Buffer.Resizable && (Offset > Buffer.Bytes.Length || !Tracking && FixedCount > (Buffer.Bytes.Length - Offset) / Size);
        internal int Count => !Buffer.Detached && !Buffer.Resizable ? FixedCount : OutOfBounds ? 0 : Tracking ? (Buffer.Bytes.Length - Offset) / Size : FixedCount;
        internal readonly string Name, Kind;
        internal TypedArrayValue(string name, BinaryBuffer buffer, int offset, int count, bool tracking = false)
        {
            Name = name; Kind = name == "Uint8ClampedArray" ? "Uint8" : name[..^5];
            Size = BinarySize(Kind); Buffer = buffer; Offset = offset; FixedCount = count; Tracking = tracking;
            prototype = (TsObject)GetTypedArrayConstructor(name).Get("prototype");
        }
        internal override TsObject PropertyPrototype => prototype;
        internal override void SetPrototype(TsObject value) { if (CanSetPrototype(value)) prototype = value; }
        internal object Read(int index) => Buffer.Detached || Buffer.Resizable && (uint)index >= (uint)Count ? Undefined : ReadBinary(Buffer.Bytes.AsSpan(Offset + index * Size, Size), Kind, BitConverter.IsLittleEndian);
        internal void Write(int index, object value)
        {
            bool bigint = Kind.StartsWith("Big", StringComparison.Ordinal);
            BigInteger integer = bigint ? BigIntValue(value, false) : default;
            double number = bigint ? 0 : Number(value);
            if (Name == "Uint8ClampedArray") number = double.IsNaN(number) ? 0 : Math.Round(Math.Clamp(number, 0, 255), MidpointRounding.ToEven);
            if ((uint)index < (uint)Count) WriteBinary(Buffer.Bytes.AsSpan(Offset + index * Size, Size), Kind, BitConverter.IsLittleEndian, number, integer);
        }
        static bool NumericKey(string key, out double index)
        {
            if (key == "-0") { index = -1; return true; }
            return double.TryParse(key, NumberStyles.Float, CultureInfo.InvariantCulture, out index) && NumberKeyString(index) == key;
        }
        internal double? ReadNumberIndex(double index)
        {
            if (!Valid(index)) return null;
            if (Kind.StartsWith("Big", StringComparison.Ordinal)) return Cast<double?>(Read((int)index), "checked conversion failed");
            return ReadBinaryNumber(Buffer.Bytes.AsSpan(Offset + (int)index * Size, Size), Kind, BitConverter.IsLittleEndian);
        }
        internal object ReadIndex(double index) => Valid(index) ? Read((int)index) : Undefined;
        internal void WriteIndex(double index, object value) => Write(Valid(index) ? (int)index : -1, value);
        bool Valid(double index) => index >= 0 && index < Count && index == Math.Truncate(index);
        internal double? ReadNumberProperty(string key)
        {
            if (key is "length" or "byteLength" or "byteOffset" or "buffer") {
                if (!base.HasOwn(key) && PropertyPrototype is TsObject prototype && prototype.TryOwnAccessor(key, out var accessor) && accessor.Getter is TypedArrayGetter getter)
                    return getter.ReadNumber(this);
            }
            return Cast<double?>(Get(key), "checked conversion failed");
        }
        public override object Get(string key)
        {
            if (key is "length" or "byteLength" or "byteOffset" or "buffer")
            {
                if (!base.HasOwn(key) && PropertyPrototype is TsObject prototype && prototype.TryOwnAccessor(key, out var accessor) && accessor.Getter is TypedArrayGetter getter)
                    return getter.Read(this);
            }
            return NumericKey(key, out double index) ? Valid(index) ? Read((int)index) : Undefined : base.Get(key);
        }
        public override object Set(string key, object value)
        {
            if (!NumericKey(key, out double index)) return base.Set(key, value);
            Write(Valid(index) ? (int)index : -1, value); return value;
        }
        public override object SetOwnValue(string key, object value) => NumericKey(key, out _) ? Set(key, value) : base.SetOwnValue(key, value);
        public override bool HasOwn(string key) => NumericKey(key, out double index) ? Valid(index) : base.HasOwn(key);
        public override bool DeleteOwn(string key) => NumericKey(key, out double index) ? !Valid(index) : base.DeleteOwn(key);
        public override object OwnKeys()
        {
            var result = new JsArray(0);
            for (int i = 0; i < Count; i++) result.Append(i.ToString(CultureInfo.InvariantCulture));
            var extra = (JsArray)base.OwnKeys();
            for (uint i = 0; i < extra.Length(); i++) result.Append(extra.ElementAt(i));
            return result;
        }
        internal override object OwnPropertyNames() => OwnKeys();
        internal override object ForInOwnKeys() => OwnKeys();
    }
    public static int Float64BufferLength(byte[] bytes) => bytes.Length / 8;
    public static byte[] TryFloat64Buffer(object value)
    {
        if (value is not TypedArrayValue array || array.Kind != "Float64" || array.Offset != 0 || array.Buffer.Detached || array.Buffer.Resizable || (long)array.FixedCount * 8 != array.Buffer.Bytes.Length || array.HasOwn("length")) return null;
        return array.PropertyPrototype is TsObject parent && parent.TryOwnAccessor("length", out var accessor) && accessor.Getter is TypedArrayGetter getter && getter.IsLength ? array.Buffer.Bytes : null;
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static double ReadFloat64Buffer(byte[] bytes, int index) => BitConverter.ToDouble(bytes.AsSpan(index * 8, 8));
    static TypedArrayValue RequireTypedArray(object value) => value as TypedArrayValue ?? throw new InvalidOperationException("Typed array receiver required");
    static TypedArrayValue RequireTypedArrayMethod(object value)
    {
        var array = RequireTypedArray(value);
        if (array.OutOfBounds) throw new InvalidOperationException("Typed array is out of bounds");
        return array;
    }
    static TypedArrayValue AllocateTypedArray(string name, int count)
    {
        int size = BinarySize(name == "Uint8ClampedArray" ? "Uint8" : name[..^5]);
        if (count > Array.MaxLength / size) throw new ArgumentOutOfRangeException(null, "Typed array is too large");
        return new TypedArrayValue(name, new BinaryBuffer(count * size), 0, count);
    }
    static List<object> TypedArrayInput(object input)
    {
        var result = new List<object>();
        bool custom = TryStartCustomIterable(input, out var iterable);
        if (custom || input is string or JsArray or JsArguments or JsKeyedCollection or JsCollectionIterator or JsSequenceIterator or JsRegexIterator || IsIteratorObject(input))
        {
            iterable = custom ? iterable : Iteration.Start(input);
            int position = 0;
            while (position < Iteration.Length(iterable))
            {
                result.Add(Iteration.Value(iterable, position));
                position += Iteration.Step(iterable, position);
            }
        }
        else
        {
            var source = ToObject(input); long length = IndexedLength(source);
            if (length > Array.MaxLength) throw new ArgumentOutOfRangeException(null, "Typed array is too large");
            for (long i = 0; i < length; i++) result.Add(IndexedValue(source, i));
        }
        return result;
    }
    sealed class TypedArrayGetter(string name, Func<TypedArrayValue, object> read) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Typed array getter requires a receiver")), null, "get " + name, 0)
    {
        internal bool IsLength => name == "length";
        internal object Read(TypedArrayValue value) => read(value);
        internal double? ReadNumber(TypedArrayValue value) => name switch {
            "length" => value.Count, "byteLength" => value.Count * value.Size,
            "byteOffset" => value.OutOfBounds ? 0 : value.Offset,
            _ => Cast<double?>(read(value), "checked conversion failed")
        };
        public override object InvokeWithReceiver(object receiver, object[] args) => read(RequireTypedArray(receiver));
    }
    sealed class TypedArrayConstructor : NativeNamespaceFunction
    {
        readonly string name;
        internal TypedArrayConstructor(string name) : base(name, static _ => throw new InvalidOperationException("Typed array requires new"), 3)
        {
            this.name = name;
            string speciesKey = PropertyKey(SpeciesSymbol); var speciesGetter = new ConstructorSpeciesGetter();
            Member(speciesKey, Undefined); DefineAccessor(speciesKey, speciesGetter, 1); speciesGetter.InferPropertyName("get [Symbol.species]");
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            int size = BinarySize(name == "Uint8ClampedArray" ? "Uint8" : name[..^5]);
            Member("BYTES_PER_ELEMENT", (double)size);
            prototype.DefineMethod("BYTES_PER_ELEMENT", (double)size);
            prototype.DefineMethod("constructor", this);
            if (name == "Uint8Array") DefineByteEncoding(this, prototype);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), name);
            void Getter(string key, Func<TypedArrayValue, object> read) => prototype.DefineClassAccessor(key, new TypedArrayGetter(key, read), 1);
            Getter("length", a => (double)a.Count); Getter("byteLength", a => (double)(a.Count * a.Size));
            Getter("byteOffset", a => (double)(a.OutOfBounds ? 0 : a.Offset)); Getter("buffer", a => a.Buffer);
            foreach (string method in new[] { "at", "join", "indexOf", "lastIndexOf", "includes", "forEach", "every", "some", "find", "findIndex", "findLast", "findLastIndex", "reduce", "reduceRight", "fill", "copyWithin", "reverse", "keys", "values", "entries" })
                prototype.DefineMethod(method, new CollectionMethod(method, ((JsFunction)ArrayMethodValue(method)).Get("length") is double length ? (int)length : 0, (receiver, args) =>
                {
                    var array = RequireTypedArrayMethod(receiver);
                    if (method is "keys" or "values" or "entries") return new TypedArrayIterator(array, method);
                    object result = method is "forEach" or "every" or "some" or "find" or "findIndex" or "findLast" or "findLastIndex" or "reduce" or "reduceRight"
                        ? TypedArrayCallback(array, method, args) : CallMethod(ArrayMethodValue(method), array, args);
                    return method is "fill" or "copyWithin" or "reverse" ? array : result;
                }));
            prototype.DefineMethod(PropertyKey(IteratorSymbol), prototype.Get("values"));
            prototype.DefineMethod("toString", new CollectionMethod("toString", 0, (receiver, _) => ArrayJoin(RequireTypedArrayMethod(receiver), Undefined)));
            foreach (string method in new[] { "slice", "subarray", "set", "map", "filter", "toReversed", "with", "sort", "toSorted", "toLocaleString" })
                prototype.DefineMethod(method, new CollectionMethod(method, method is "slice" or "subarray" or "with" ? 2 : method is "toLocaleString" or "toReversed" ? 0 : 1, (receiver, args) => TypedArrayMethod(RequireTypedArrayMethod(receiver), method, args)));
            Member("of", new CollectionMethod("of", 0, (receiver, args) => TypedArrayFromValues(receiver, args)));
            Member("from", new CollectionMethod("from", 1, (receiver, args) =>
            {
                object mapper = FunctionArgument(args, 1);
                if (!IsUndefined(mapper) && mapper is not JsFunction) throw new InvalidOperationException("TypedArray.from mapper must be callable");
                var values = TypedArrayInput(FunctionArgument(args, 0));
                var result = CreateTypedArrayResult(receiver, values.Count);
                for (int i = 0; i < values.Count; i++) result.Write(i, IsUndefined(mapper) ? values[i] : CallMethod(mapper, FunctionArgument(args, 2), new object[] { values[i], (double)i }));
                return result;
            }));
        }
        public override object Construct(object[] args)
        {
            object input = FunctionArgument(args, 0);
            if (input is BinaryBuffer buffer)
            {
                int size = BinarySize(name == "Uint8ClampedArray" ? "Uint8" : name[..^5]);
                int offset = BinaryIndex(FunctionArgument(args, 1));
                EnsureAttached(buffer);
                if (offset % size != 0 || offset > buffer.Bytes.Length) throw new ArgumentOutOfRangeException(null, "Typed array offset is invalid");
                int length;
                if (IsUndefined(FunctionArgument(args, 2)))
                {
                    if (!buffer.Resizable && (buffer.Bytes.Length - offset) % size != 0) throw new ArgumentOutOfRangeException(null, "Buffer length is not aligned");
                    length = (buffer.Bytes.Length - offset) / size;
                }
                else length = BinaryIndex(args[2]);
                EnsureAttached(buffer);
                if (length > (buffer.Bytes.Length - offset) / size) throw new ArgumentOutOfRangeException(null, "Typed array length is invalid");
                return new TypedArrayValue(name, buffer, offset, length, buffer.Resizable && IsUndefined(FunctionArgument(args, 2)));
            }
            if (input is not TsObject) return AllocateTypedArray(name, BinaryIndex(input));
            var values = TypedArrayInput(input);
            var result = AllocateTypedArray(name, values.Count);
            for (int i = 0; i < values.Count; i++) result.Write(i, values[i]);
            return result;
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
    static TypedArrayValue CreateTypedArrayResult(object receiver, int count)
    {
        if (receiver is not JsFunction constructor || constructor.Construct(new object[] { (double)count }) is not TypedArrayValue result || result.OutOfBounds || result.Count < count)
            throw new InvalidOperationException("Typed array constructor required");
        return result;
    }
    static object TypedArrayFromValues(object receiver, object[] values)
    {
        var result = CreateTypedArrayResult(receiver, values.Length);
        for (int i = 0; i < values.Length; i++) result.Write(i, values[i]);
        return result;
    }
    static class TypedArrayComparison
    {
        internal static readonly JsFunction Value = new((FunctionBody)(static args =>
        {
            if (args[0] is BigInteger left) return (double)left.CompareTo((BigInteger)args[1]);
            double a = (double)args[0], b = (double)args[1];
            if (double.IsNaN(a)) return double.IsNaN(b) ? 0d : 1d;
            if (double.IsNaN(b)) return -1d;
            if (a == 0 && b == 0) return (double)(double.IsNegative(b).CompareTo(double.IsNegative(a)));
            return (double)a.CompareTo(b);
        }), null, "", 2);
    }
    static object TypedArrayMethod(TypedArrayValue array, string method, object[] args)
    {
        if (method is "slice" or "subarray")
        {
            int originalLength = array.Count;
            int first = BufferSliceIndex(FunctionArgument(args, 0), originalLength);
            int end = IsUndefined(FunctionArgument(args, 1)) ? originalLength : BufferSliceIndex(args[1], originalLength);
            int count = Math.Max(end - first, 0);
            if (method == "subarray") return TypedArraySpeciesCreate(array, count, array.Buffer, array.Offset + first * array.Size, array.Tracking && IsUndefined(FunctionArgument(args, 1)));
            var result = TypedArraySpeciesCreate(array, count);
            if (count == 0) return result;
            if (array.OutOfBounds) throw new InvalidOperationException("Typed array is out of bounds");
            int available = Math.Min(count, Math.Max(array.Count - first, 0));
            if (array.Kind.StartsWith("Big", StringComparison.Ordinal) != result.Kind.StartsWith("Big", StringComparison.Ordinal)) throw new InvalidOperationException("Typed array content types do not match");
            if (array.Name == result.Name)
            {
                var sourceBytes = array.Buffer.Bytes.AsSpan(array.Offset + Math.Min(first, array.Count) * array.Size, available * array.Size);
                var targetBytes = result.Buffer.Bytes.AsSpan(result.Offset, count * result.Size);
                if (ReferenceEquals(array.Buffer, result.Buffer))
                    for (int i = 0; i < sourceBytes.Length; i++) targetBytes[i] = sourceBytes[i];
                else sourceBytes.CopyTo(targetBytes);
            }
            else for (int i = 0; i < available; i++) result.Write(i, array.Read(first + i));
            return result;
        }
        if (method == "set")
        {
            int offset = BinaryIndex(FunctionArgument(args, 1));
            var source = ToObject(FunctionArgument(args, 0));
            if (source is TypedArrayValue invalid && invalid.OutOfBounds) throw new InvalidOperationException("Source typed array is out of bounds");
            long length = IndexedLength(source);
            if (length > array.Count - offset) throw new ArgumentOutOfRangeException(null, "Typed array set exceeds target length");
            if (source is TypedArrayValue typed)
            {
                var copy = new object[(int)length];
                for (int i = 0; i < copy.Length; i++) copy[i] = typed.Read(i);
                for (int i = 0; i < copy.Length; i++) array.Write(offset + i, copy[i]);
            }
            else for (int i = 0; i < length; i++) array.Write(offset + i, IndexedValue(source, i));
            return Undefined;
        }
        if (method == "toLocaleString") return CallMethod(ArrayMethodValue(method), array, args);
        if (method is "sort" or "toSorted")
        {
            object comparator = FunctionArgument(args, 0);
            if (!IsUndefined(comparator) && comparator is not JsFunction) throw new InvalidOperationException("Typed array comparator must be callable");
            var values = new object[array.Count];
            for (int i = 0; i < values.Length; i++) values[i] = array.Read(i);
            StableSort(values, new object[values.Length], 0, values.Length, comparator as JsFunction ?? TypedArrayComparison.Value);
            var result = method == "sort" ? array : AllocateTypedArray(array.Name, array.Count);
            for (int i = 0; i < values.Length; i++) result.Write(i, values[i]);
            return result;
        }
        if (method is "map" or "filter")
        {
            object callback = FunctionArgument(args, 0);
            if (callback is not JsFunction) throw new InvalidOperationException("Typed array callback must be callable");
            int length = array.Count;
            var result = method == "map" ? TypedArraySpeciesCreate(array, length) : null;
            var selected = method == "filter" ? new List<object>() : null;
            for (int i = 0; i < length; i++)
            {
                object value = array.Read(i), mapped = CallMethod(callback, FunctionArgument(args, 1), new object[] { value, (double)i, array });
                if (result != null) result.Write(i, mapped); else if (Truthy(mapped)) selected.Add(value);
            }
            if (result != null) return result;
            result = TypedArraySpeciesCreate(array, selected.Count);
            for (int i = 0; i < selected.Count; i++) result.Write(i, selected[i]);
            return result;
        }
        var copied = AllocateTypedArray(array.Name, array.Count);
        for (int i = 0; i < array.Count; i++) copied.Write(i, array.Read(method == "toReversed" ? array.Count - i - 1 : i));
        if (method == "with")
        {
            double index = Math.Truncate(Number(FunctionArgument(args, 0))); if (double.IsNaN(index)) index = 0;
            if (index < 0) index += array.Count;
            if (index < 0 || index >= array.Count) throw new ArgumentOutOfRangeException(null, "Typed array index is out of range");
            copied.Write((int)index, FunctionArgument(args, 1));
        }
        return copied;
    }
}
