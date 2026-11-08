using System;
using System.Numerics;

public static partial class Js
{
    static TypedArrayValue AtomicArray(object value)
    {
        var array = RequireTypedArray(value);
        if (array.Name == "Uint8ClampedArray" || array.Kind.StartsWith("Float", StringComparison.Ordinal)) throw new InvalidOperationException("Atomics requires an integer typed array");
        if (array.OutOfBounds) throw new InvalidOperationException("Atomic view is out of bounds");
        return array;
    }
    static object AtomicGate(TypedArrayValue array) => array.Buffer is SharedBuffer shared ? shared.Storage : array.Buffer;
    static void AtomicBounds(TypedArrayValue array, int index)
    {
        if (array.OutOfBounds) throw new InvalidOperationException("Atomic view is out of bounds");
        if (index >= array.Count) throw new ArgumentOutOfRangeException(nameof(index), "Atomic index is out of range");
    }
    static object AtomicValue(TypedArrayValue array, object value)
    {
        if (array.Kind.StartsWith("Big", StringComparison.Ordinal)) return BigIntValue(value, false);
        double number = Number(value);
        return double.IsNaN(number) || number == 0 ? 0d : Math.Truncate(number);
    }
    static object AtomicCell(TypedArrayValue array, object value)
    {
        Span<byte> bytes = stackalloc byte[8]; var cell = bytes[..array.Size];
        WriteBinary(cell, array.Kind, BitConverter.IsLittleEndian, value is double number ? number : 0d, value is BigInteger integer ? integer : default);
        return ReadBinary(cell, array.Kind, BitConverter.IsLittleEndian);
    }
    static object AtomicUpdate(string operation, object[] args)
    {
        var array = AtomicArray(FunctionArgument(args, 0)); int index = BinaryIndex(FunctionArgument(args, 1)); AtomicBounds(array, index);
        object value = operation == "load" ? Undefined : AtomicValue(array, FunctionArgument(args, 2));
        object replacement = operation == "compareExchange" ? AtomicValue(array, FunctionArgument(args, 3)) : Undefined;
        object expected = operation == "compareExchange" ? AtomicCell(array, value) : Undefined;
        lock (AtomicGate(array)) {
            AtomicBounds(array, index);
            object old = array.Read(index);
            if (operation == "load") return old;
            if (operation == "compareExchange") { if (Equals(old, expected)) array.Write(index, replacement); return old; }
            if (operation is "store" or "exchange") { array.Write(index, value); return operation == "store" ? value : old; }
            object result;
            if (old is BigInteger left) {
                var right = (BigInteger)value;
                result = operation switch { "add" => left + right, "sub" => left - right, "and" => left & right, "or" => left | right, "xor" => left ^ right, _ => throw new InvalidOperationException("Unknown atomic operation") };
            } else {
                int first = ToInt32((double)old), second = ToInt32((double)value);
                result = (double)(operation switch { "add" => unchecked(first + second), "sub" => unchecked(first - second), "and" => first & second, "or" => first | second, "xor" => first ^ second, _ => throw new InvalidOperationException("Unknown atomic operation") });
            }
            array.Write(index, result); return old;
        }
    }
    static TsObject CreateAtomics()
    {
        var result = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype };
        foreach (string operation in new[] { "add", "sub", "and", "or", "xor", "load", "store", "exchange", "compareExchange" }) {
            int length = operation == "load" ? 2 : operation == "compareExchange" ? 4 : 3;
            result.DefineMethod(operation, new JsFunction((FunctionBody)(args => AtomicUpdate(operation, args)), null, operation, length));
        }
        result.DefineMethod("wait", new JsFunction((FunctionBody)AtomicWait, null, "wait", 4));
        result.DefineMethod("waitAsync", new JsFunction((FunctionBody)AtomicWaitAsync, null, "waitAsync", 4));
        result.DefineMethod("notify", new JsFunction((FunctionBody)AtomicNotify, null, "notify", 3));
        result.DefineMethod("pause", new JsFunction((FunctionBody)(static args => {
            object hint = FunctionArgument(args,0);
            if (!IsUndefined(hint) && (hint is not double number || !double.IsFinite(number) || Math.Truncate(number) != number))
                throw new InvalidOperationException("Atomics.pause hint must be an integer number");
            System.Threading.Thread.SpinWait(1); return Undefined;
        }), null, "pause", 0));
        // The current implementation uses CLR locks for every integer width.
        result.DefineMethod("isLockFree", new JsFunction((FunctionBody)(static args => { _ = Number(FunctionArgument(args, 0)); return BoxBoolean(false); }), null, "isLockFree", 1));
        result.DefineMethod(PropertyKey(ToStringTagSymbol), "Atomics", false); return result;
    }
    static class AtomicsNamespace { internal static readonly TsObject Value = CreateAtomics(); }
}
