using System.Numerics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// Support for operations that cannot be resolved statically. Typed numeric code
// does not pass through this representation.
public abstract partial class TsObject
{
    private OwnProperties extra;
    internal int OwnPropertyVersion => extra?.Version ?? 0;

    public virtual bool TrySetNumberProperty(string key, double? value) => false;
    protected bool TrySetStoredNumber(string key, double? value) => !IsModuleNamespace && this is JsDescriptorObject descriptor && !descriptor.HasPropertyDescriptors &&
        !TryOwnAccessor(key, out _) && extra != null && extra.TrySetNumber(key, value);

    public virtual bool HasOwn(string key) => extra != null && extra.ContainsKey(key);
    public virtual bool HasProperty(string key) => HasOwn(key) || PropertyPrototype is not null && PropertyPrototype.HasProperty(key);

    public virtual object Get(string key) =>
        TryOwnAccessor(key,out var accessor)?accessor.Read(this):extra != null && extra.TryGetValue(key, out var value) ? value : InheritedValue(key, this);

    public virtual double? GetNumberProperty(string key) => Js.Cast<double?>(Get(key), "checked conversion failed");

    public virtual object Set(string key, object value)
    {
        if (IsModuleNamespace) throw new InvalidOperationException("Module namespace is read-only");
        if(TryOwnAccessor(key,out var accessor))return accessor.Write(this,value);
        if (IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Property is read-only");
        if (SetInheritedProperty(key, value)) return value;
        if (!IsExtensible && !HasOwn(key)) throw new InvalidOperationException("Object is not extensible");
        (extra ??= new OwnProperties())[key] = value;
        return value;
    }
}

public static partial class Js
{
    public static int Run(Action entry)
    {
        try
        {
            entry();
            do { DrainPromiseJobs(); } while (RunNextTimer());
            CheckModuleEvaluations(); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally { consoleGroupDepth = 0; ClearHostEvents(); ClearTimers(); promiseJobs?.Clear(); unhandledPromises?.Clear(); ClearKeptWeakTargets(); ClearModuleEvaluations(); ClearObjectUrls(); }
    }
    private sealed class UndefinedValue { }
    public static readonly object Undefined = new UndefinedValue();
    static readonly object BoxedTrue = true;
    static readonly object BoxedFalse = false;
    public static object GetUndefined() => Undefined;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumber(object value) => value is double;
    public static bool IsUndefined(object value) => ReferenceEquals(value, Undefined);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckInitialized(bool initialized)
    {
        if (!initialized) throw new InvalidOperationException("Cannot access binding before initialization");
    }
    public static bool IsNullish(object value) => value == null || ReferenceEquals(value, Undefined);
    public static void RequireObject(object value)
    {
        if (IsNullish(value)) throw new InvalidOperationException("Cannot destructure null or undefined");
    }
    // Called only after the compiler guards the upper bound and excludes NaN.
    public static int LoopLimit(double bound) => bound > 0 ? (int)Math.Ceiling(bound) : 0;
    public static string TypeOf(object value) => value switch
    {
        null => "object",
        bool => "boolean",
        JsSymbol => "symbol",
        BigInteger => "bigint",
        double => "number",
        string => "string",
        JsFunction => "function",
        Delegate => "function",
        _ => ReferenceEquals(value, Undefined) ? "undefined" : "object"
    };
    static class PerformanceClock
    {
        internal static readonly long Start = Stopwatch.GetTimestamp();
        internal static readonly double Origin = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
    public static double Now()
    {
        long start = PerformanceClock.Start;
        return (Stopwatch.GetTimestamp() - start) * (1000.0 / Stopwatch.Frequency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToInt32(double value)
    {
        if (System.Runtime.Intrinsics.X86.Sse2.IsSupported) {
            int result = System.Runtime.Intrinsics.X86.Sse2.ConvertToInt32WithTruncation(
                System.Runtime.Intrinsics.Vector128.CreateScalar(value));
            if (result != int.MinValue) return result;
            return ToInt32Slow(value);
        }
        if (value >= int.MinValue && value <= int.MaxValue) return (int)value;
        return ToInt32Slow(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ToInt32Slow(double value)
    {
        if (!double.IsFinite(value)) return 0;
        double reduced = Math.Truncate(value) % 4294967296.0;
        if (reduced < 0) reduced += 4294967296.0;
        return unchecked((int)(uint)reduced);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Cast<T>(object value, string message)
    {
        if (value is T result) return result;
        if (value == null && !typeof(T).IsValueType) return default;
        // Nullable value types are the value types whose default boxes to null.
        if (ReferenceEquals(value, Undefined) && typeof(T).IsValueType && default(T) is null) return default;
        throw new InvalidCastException(message);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Check<T>(object value, string message) where T : class =>
        value == null || ReferenceEquals(value, Undefined) || value is T ? value : throw new InvalidCastException(message);
    public static int StringLength(object value) =>
        value is string text ? text.Length : throw new InvalidOperationException("Expected string");

    // Empty nullable values mean JS undefined; CLR null stays a separate JS value.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? WrapNumber(double value) => value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? WrapBoolean(bool value) => value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? EmptyNumber() => null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool? EmptyBoolean() => null;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object BoxNumber(double? value) => value.HasValue ? (object)value.Value : Undefined;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object BoxBoolean(bool? value) => !value.HasValue ? Undefined : value.Value ? BoxedTrue : BoxedFalse;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double NumberValue(double? value) => value.GetValueOrDefault(double.NaN);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BooleanValue(bool? value) => value.GetValueOrDefault();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberMissing(double? value) => !value.HasValue;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BooleanMissing(bool? value) => !value.HasValue;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool NumberTruthy(double? value) => value.HasValue && value.Value != 0 && !double.IsNaN(value.Value);

    public static double Number(object value) => value switch
    {
        double n => n,
        JsSymbol => throw new InvalidOperationException("Cannot convert a symbol to a number"),
        BigInteger => throw new InvalidOperationException("Cannot implicitly convert a bigint to a number"),
        null => 0,
        bool b => b ? 1 : 0,
        string s => StringNumber(s),
        TsObject obj => Number(ToPrimitive(obj, "number")),
        _ => double.NaN
    };
    public static bool Boolean(object value) => Cast<bool>(value, "Expected boolean");
    public static bool Truthy(object value) => value switch
    {
        null => false,
        bool b => b,
        BigInteger n => !n.IsZero,
        double n => n != 0 && !double.IsNaN(n),
        string s => s.Length != 0,
        _ => !ReferenceEquals(value, Undefined)
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object Get(object receiver, string key) => receiver switch
    {
        TsObject obj => obj.Get(key),
        string text when key == "length" => (double)text.Length,
        string or double or bool or JsSymbol or BigInteger => PrimitivePrototype(receiver).GetWithReceiver(key, receiver),
        _ => throw new InvalidOperationException("Cannot read property of undefined")
    };

    public static void SetNumber(object receiver, string key, double? value)
    {
        if (receiver is TsObject obj && obj.TrySetNumberProperty(key, value)) return;
        Set(receiver, key, BoxNumber(value));
    }

    public static object Set(object receiver, string key, object value) =>
        receiver is TsObject obj ? obj.Set(key, value) :
        throw new InvalidOperationException("Property write requires an object");

    public static bool HasOwn(object receiver, object key)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("Cannot inspect properties of null or undefined");
        return key is double number ? HasOwnNumber(receiver, number) : HasOwnNamed(receiver, PropertyKey(key));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasOwnNumber(object receiver, double? key)
    {
        if (key.HasValue)
        {
            double index = key.GetValueOrDefault();
            if (receiver is JsArray array && index >= 0 && index < uint.MaxValue && index == (uint)index)
                return array.HasElement((uint)index);
            if (receiver is string text) return index >= 0 && index < text.Length && index == (uint)index;
        }
        return HasOwnNamed(receiver, key.HasValue ? PropertyKey(key.GetValueOrDefault()) : "undefined");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasOwnIndex(object receiver, int key)
    {
        if (receiver is JsArray array && key >= 0) return array.HasElement((uint)key);
        if (receiver is string text) return (uint)key < (uint)text.Length;
        return HasOwnNamed(receiver, Text((double)key));
    }

    static bool HasOwnNamed(object receiver, string name)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("Cannot inspect properties of null or undefined");
        if (receiver is TsObject obj) return obj.HasOwn(name);
        if (receiver is string text) return name == "length" ||
            JsArray.Index(name, out uint index) && index < text.Length;
        return false;
    }

    public static double? GetNumber(object receiver, string key)
    {
        if (receiver is TypedArrayValue typed) return typed.ReadNumberProperty(key);
        if (key == "length") {
            if (receiver is string text) return text.Length;
            if (receiver is JsArray array) return array.Length();
        }
        return receiver is TsObject obj ? obj.GetNumberProperty(key) : Cast<double?>(Get(receiver, key), "checked conversion failed");
    }
    public static double? IndexGetNumber(object receiver, double? index)
    {
        if (index.HasValue) {
            if (receiver is NumberArray numbers) return numbers.GetNumber(index.GetValueOrDefault());
            if (receiver is TypedArrayValue typed) return typed.ReadNumberIndex(index.GetValueOrDefault());
            if (receiver is TaggedArray tagged && index.Value >= 0 && index.Value <= int.MaxValue && index.Value == (int)index.Value)
                return tagged.GetNumberAt((int)index.Value);
        }
        return Cast<double?>(IndexGetNumberKey(receiver, index), "checked conversion failed");
    }
    public static object ArgumentsIndexGetNumberKey(object receiver, double? index)
    {
        var arguments = (JsArguments)receiver;
        if (index.HasValue && arguments.TryReadIndex(index.Value, out object value)) return value;
        return IndexGetNumberKey(receiver, index);
    }
    public static object ArrayIndexGetNumberKey(object receiver, double? index)
    {
        if (receiver is JsArray array && index.HasValue) {
            double number = index.Value;
            // These built-in representations use the ordinary numeric
            // property lookup. Unknown CLR subclasses keep virtual Get.
            if (number >= 0 && number < uint.MaxValue && number == (uint)number &&
                (array.GetType() == typeof(JsArray) || array is NumberArray or TaggedArray or ReversibleArray or JsTemplateArray))
                return array.ElementAt((uint)number);
        }
        return IndexGetNumberKey(receiver, index);
    }
    public static object IndexGetNumberKey(object receiver, double? index)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("Cannot read property of null or undefined");
        if (receiver is TypedArrayValue typed && index.HasValue) return typed.ReadIndex(index.Value);
        string text = index.HasValue ? NumberKeyString(index.Value) : "undefined";
        if (receiver is JsArray array) return array.Get(text);
        if (receiver is string value && JsArray.Index(text, out uint position) && position < value.Length)
            return Iteration.CodeUnit(value[(int)position]);
        return Get(receiver, text);
    }
    public static object IndexGet(object receiver, object key)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("Cannot read property of null or undefined");
        if (receiver is JsArray array) return array.GetIndex(key);
        if (receiver is TypedArrayValue typed && key is double number) return typed.ReadIndex(number);
        string text = PropertyKey(key);
        if (receiver is string value && JsArray.Index(text, out uint index) && index < value.Length)
            return Iteration.CodeUnit(value[(int)index]);
        return Get(receiver, text);
    }
    public static void IndexSet(object receiver, object key, object value)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("Cannot write property of null or undefined");
        if (receiver is JsArray array) array.SetIndex(key, value);
        else if (receiver is TypedArrayValue typed && key is double number) typed.WriteIndex(number, value);
        else Set(receiver, PropertyKey(key), value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ArrayPushOne(object receiver, object value)
    {
        if (receiver is not JsArray array) throw new InvalidOperationException("push requires an array");
        if (!array.CanAppendDirectly) return ArrayPush(array, new[] {value});
        array.Append(value);
        return array.Length();
    }
    public static double ArrayPushOneReserved(object receiver, object value, double hint)
    {
        // Reserve only when the push actually executes, after its argument.
        // A hint changes capacity, never array length or occupied indices.
        if (receiver is JsArray array && array.Length() == 0 && hint > 0 && hint <= 1048576)
            array.ReserveCapacity((int)Math.Ceiling(hint));
        return ArrayPushOne(receiver, value);
    }
    public static double ArrayPush(object receiver, object[] values)
    {
        if (receiver is JsArray array && array.CanAppendDirectly) return array.PushMany(values);
        var source = ToObject(receiver);
        long length = IndexedLength(source);
        if (length > 9007199254740991L - values.Length) throw new InvalidOperationException("Array-like length exceeds the safe integer limit");
        foreach (object value in values) source.SetWithReceiver((length++).ToString(CultureInfo.InvariantCulture), value, source);
        source.SetWithReceiver("length", (double)length, source);
        return length;
    }
    public static bool ArrayReverseBuiltin(object receiver)
    {
        if (IsNullish(receiver)) throw new InvalidOperationException("Cannot read reverse of null or undefined");
        return receiver is JsArray array && !array.HasOwn("reverse");
    }
    public static object ArrayReverse(object receiver, bool builtin)
    {
        if (receiver is not JsArray array) throw new InvalidOperationException("Array reverse requires an array");
        if (!builtin) throw new InvalidOperationException("reverse is not callable");
        return array.Reverse();
    }

    public static double ArraySearch(object receiver, object value, object fromIndex, bool includes) =>
        receiver is JsArray array ? array.Search(value, fromIndex, includes) : receiver is string ? StringSearchConverted(includes ? StringSearchKind.Includes : StringSearchKind.IndexOf, receiver, value, fromIndex) : throw new InvalidOperationException("Array search requires an array");
    public static double ArraySearchNumber(object receiver, double? value, object fromIndex, bool includes) =>
        receiver is JsArray array ? array.SearchNumber(value, fromIndex, includes) : receiver is string ? StringSearchConverted(includes ? StringSearchKind.Includes : StringSearchKind.IndexOf, receiver, BoxNumber(value), fromIndex) : throw new InvalidOperationException("Array search requires an array");

    public static object Slice(object receiver, object start, object end)
    {
        if (receiver is JsArray) return SliceArrayLike(receiver, start, end);
        if (receiver is string text)
        {
            uint first = JsArray.SliceBound(start, (uint)text.Length);
            uint last = IsUndefined(end) ? (uint)text.Length : JsArray.SliceBound(end, (uint)text.Length);
            return text.Substring((int)first, last > first ? (int)(last - first) : 0);
        }
        throw new InvalidOperationException("slice requires an array or string");
    }
    public static void ArrayAppend(object receiver, object value) => ((JsArray)receiver).Append(value);
    public static void ArrayReserveCapacity(object receiver, double limit)
    {
        if (receiver is JsArray array && limit > 0 && limit <= int.MaxValue) array.ReserveCapacity((int)Math.Ceiling(limit));
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object[] ArrayTryDense(object receiver, double limit)
    {
        if (receiver is JsArray guarded && !guarded.CanUseElementStorage) return null;
        if (receiver is JsArray array && receiver.GetType() == typeof(JsArray)) return array.DenseRange(limit);
        if (receiver is TaggedArray tagged) return tagged.MaterializedDenseRange(limit);
        return receiver is ReversibleArray view ? view.DenseRange(limit) : null;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object ArrayDecode(object value) => JsArray.DecodeElement(value);
    // The compiler proves these indexes are non-negative.
    public static object ArrayGetAt(object receiver, int index) => receiver is JsArray array ? array.ElementAt((uint)index) : IndexGet(receiver, (double)index);
    public static void ArraySetAt(object receiver, int index, object value)
    {
        if (receiver is JsArray array) array.SetAt(index, value);
        else IndexSet(receiver, (double)index, value);
    }
    public static void ArrayHole(object receiver) => ((JsArray)receiver).AppendHole();
    public static void ArraySpread(object receiver, object source, int extra) => ((JsArray)receiver).Spread(source, extra);
    public static object CallRest(object arguments, int start, bool numeric)
    {
        var source = (JsArray)arguments;
        if (start == 0 && (!numeric || source is NumberArray)) return source;
        JsArray result = numeric ? new NumberArray(0) : new JsArray(0);
        if (result is NumberArray destination) destination.ReservePushes(Math.Max(0, source.Length() - start));
        for (uint i = (uint)start; i < source.Length(); i++)
        {
            if (result is NumberArray numbers && source is NumberArray input) numbers.PushNumber(input.GetNumberAt(unchecked((int)i)));
            else result.Append(source.ElementAt(i));
        }
        return result;
    }
    public static double PushArguments(object receiver, object arguments)
    {
        var target = (JsArray)receiver;var source = (JsArray)arguments;
        if (target is NumberArray numbers && source is NumberArray input && numbers.AppendPacked(input, 0)) return numbers.Length();
        uint length = (uint)source.Length();
        for (uint i = 0; i < length; i++) target.Append(source.ElementAt(i));
        return target.Length();
    }
    public static void LogArguments(object arguments)
    {
        var source = (JsArray)arguments;var values = new object[checked((int)source.Length())];
        for (int i = 0; i < values.Length; i++) values[i] = source.ElementAt((uint)i);
        ConsoleLog(values);
    }
    public static object ArrayPop(object receiver)
    {
        if (receiver is JsArray array) return array.Pop();
        var source = ToObject(receiver);
        long length = IndexedLength(source);
        object result = Undefined;
        if (length > 0)
        {
            length--; result = IndexedValue(source, length);
            if (!source.DeleteOwn(length.ToString(CultureInfo.InvariantCulture))) throw new InvalidOperationException("Cannot delete array-like property");
        }
        source.SetWithReceiver("length", (double)length, source);
        return result;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? ArrayGetNumber(object receiver, double index)
    {
        if (receiver is NumberArray numbers) return numbers.GetNumber(index);
        if (receiver is TaggedArray tagged && index >= 0 && index <= int.MaxValue && index == (int)index)
            return tagged.GetNumberAt((int)index);
        if (receiver is JsArray array && array.GetType() == typeof(JsArray) &&
            index >= 0 && index < uint.MaxValue && index == Math.Truncate(index))
            return Cast<double?>(array.ElementAt((uint)index), "Array element is not a number");
        return Cast<double?>(IndexGet(receiver, index), "Array element is not a number");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArraySetNumber(object receiver, double index, double? value)
    {
        if (receiver is NumberArray numbers) numbers.SetNumber(index, value);
        else if (index >= 0 && index <= int.MaxValue && Math.Truncate(index) == index && receiver is TaggedArray tagged)
            tagged.SetNumberAt((int)index, value);
        else IndexSet(receiver, index, BoxNumber(value));
    }
    public static double ArrayLength(object receiver) =>
        receiver is JsArray array ? array.Length() : throw new InvalidOperationException("Expected array");
    public static void ArraySetLength(object receiver, double length)
    {
        if (receiver is not JsArray array) throw new InvalidOperationException("Expected array");
        array.SetLength(length);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? ArrayGetNumberAt(object receiver, int index)
    {
        if (receiver is NumberArray numbers) return numbers.GetNumberAt(index);
        if (receiver is TaggedArray tagged) return tagged.GetNumberAt(index);
        if (index >= 0 && receiver is JsArray array && array.GetType() == typeof(JsArray))
            return Cast<double?>(array.ElementAt((uint)index), "Array element is not a number");
        return Cast<double?>(IndexGet(receiver, (double)index), "Array element is not a number");
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ArraySetNumberAt(object receiver, int index, double? value)
    {
        if (receiver is NumberArray numbers) numbers.SetNumberAt(index, value);
        else if (index >= 0 && receiver is TaggedArray tagged) tagged.SetNumberAt(index, value);
        else ArraySetNumber(receiver, index, value);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ArrayPushNumber(object receiver, double? value) => receiver is NumberArray numbers ? numbers.PushNumber(value) :
        ArrayPushOne(receiver, BoxNumber(value));
    public static double? ArrayPopNumber(object receiver) => receiver is NumberArray numbers ? numbers.PopNumber() :
        Cast<double?>(ArrayPop(receiver), "Array element is not a number");
    // The caller proves that the loop cannot write or invoke user code.
    public static double[] ArrayTryPackedRead(object receiver) => receiver switch
    {
        NumberArray numbers when numbers.CanUseElementStorage => numbers.TryPacked(numbers.Length()),
        TaggedArray tagged => tagged.TryPackedNumbers(tagged.Length()),
        _ => null
    };
    public static double[] ArrayTryPacked(object receiver, double limit) => receiver is NumberArray numbers && !numbers.ElementsFrozen ? numbers.TryPacked(limit) : null;
    public static void ArrayReservePushes(object receiver, double additions)
    {
        if (receiver is NumberArray numbers) numbers.ReservePushes(additions);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CanonicalNumber(double value) => double.IsNaN(value) ? double.NaN : value;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ForOfLength(object value) => Iteration.Length(value);
    public static object IterationStart(object value) => Iteration.Start(value);
    public static object ForOfValue(object value, int position) => Iteration.Value(value, position);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double? ForOfNumber(object value, int position) => Iteration.Number(value, position);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ForOfStep(object value, int position) => Iteration.Step(value, position);
    public static object ForOfSkip(object value, int position) => Iteration.Skip(value,position);

    public static double? AddToNumber(double? left, object right)
    {
        if (right is double number) return NumberValue(left) + number;
        if (right is TsObject obj)
        {
            right = ToPrimitive(obj, "default");
            if (right is double converted) return NumberValue(left) + converted;
        }
        if (right == null) return NumberValue(left) + 0.0;
        if (right is bool boolean) return NumberValue(left) + (boolean ? 1.0 : 0.0);
        if (ReferenceEquals(right, Undefined)) return NumberValue(left) + double.NaN;
        return Cast<double?>(Add(BoxNumber(left), right), "checked conversion failed");
    }

    public static object Add(object left, object right)
    {
        if (left is TsObject first) left = ToPrimitive(first, "default");
        if (right is TsObject second) right = ToPrimitive(second, "default");
        if (left is JsSymbol || right is JsSymbol) throw new InvalidOperationException("Cannot convert a symbol to a number or string");
        if (left is string || right is string) return Text(left) + Text(right);
        if (left is BigInteger a && right is BigInteger b) return a + b;
        return Number(left) + Number(right);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualNumbers(double? left, double? right) =>
        left.HasValue == right.HasValue && (!left.HasValue || left.GetValueOrDefault() == right.GetValueOrDefault());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualBooleans(bool? left, bool? right) => left == right;

    public static double Power(double value, double exponent)
    {
        int integer = (int)exponent;
        if ((uint)integer <= 16 && exponent == integer)
        {
            if (integer == 0) return 1;
            if (integer == 1) return value;
            if (integer == 2) return value * value;
            // Every intermediate is exact: 8^16 is only 2^48.
            if (value >= -8 && value <= 8 && value == (int)value)
            {
                double result = 1;
                while (integer != 0)
                {
                    if ((integer & 1) != 0) result *= value;
                    integer >>= 1;
                    if (integer != 0) value *= value;
                }
                return result;
            }
        }
        // ECMAScript differs from Math.Pow for unit bases and nonfinite exponents.
        if (double.IsNaN(exponent) || Math.Abs(value) == 1 && double.IsInfinity(exponent)) return double.NaN;
        return Math.Pow(value, exponent);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Remainder(double value, double divisor)
    {
        // Preserve general floating-point semantics outside this exact integer case.
        // Excluding zero also preserves the sign of negative zero in the fallback.
        if (divisor >= 1 && divisor <= int.MaxValue && value > 0 && value <= int.MaxValue)
        {
            int modulus = (int)divisor;
            int integer = (int)value;
            if (modulus == divisor && integer == value && (modulus & (modulus - 1)) == 0)
                return integer & (modulus - 1);
        }
        return value % divisor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equal(object left, object right)
    {
        // Number equality must still reject NaN, including an aliased box.
        if (left is double a) return right is double b && a == b;
        return ReferenceEquals(left, right) || EqualDistinct(left, right);
    }
    static bool EqualDistinct(object left, object right)
    {
        if (left is string x && right is string y) return x == y;
        if (left is BigInteger m && right is BigInteger n) return m == n;
        if (left is bool p && right is bool q) return p == q;
        return false;
    }

    public static string Text(object value) => value switch
    {
        double n when double.IsNaN(n) => "NaN",
        double n when double.IsPositiveInfinity(n) => "Infinity",
        double n when double.IsNegativeInfinity(n) => "-Infinity",
        double n when n == 0 => "0",
        double n => NumberKeyString(n),
        bool b => b ? "true" : "false",
        string s => s,
        JsSymbol symbol => symbol.Display,
        BigInteger integer => integer.ToString(CultureInfo.InvariantCulture),
        TsObject => "[object Object]",
        _ when ReferenceEquals(value, Undefined) => "undefined",
        null => "null",
        _ => throw new InvalidOperationException("Unsupported runtime value")
    };

    private static string Inspect(object value) =>
        value is BigInteger integer ? integer.ToString(CultureInfo.InvariantCulture) + "n" :
        value is double n && n == 0 && BitConverter.DoubleToInt64Bits(n) < 0 ? "-0" : Text(value);

    static string ConsoleMessage(object[] values) => string.Join(" ", Array.ConvertAll(values, Inspect));
    public static void Log(params object[] values) => WriteConsole(ConsoleMessage(values));

    public static void Discard(object value) { }
}
