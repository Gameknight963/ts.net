using System;
using System.Globalization;

public static partial class Js
{
    internal static long IndexedLength(TsObject source)
    {
        double count = Number(source.Get("length"));
        return double.IsNaN(count) || count <= 0 ? 0 : (long)Math.Min(9007199254740991d, Math.Floor(count));
    }
    static bool TryIndexedValue(TsObject source, long index, out object value)
    {
        value = Undefined;
        if (source is JsArray array)
        {
            if (array.HasElement((uint)index)) { value = array.ElementAt((uint)index); return true; }
            var prototype = array.PropertyPrototype;
            if (ReferenceEquals(prototype, ArrayPrototype) && ArraySlicePrototypesHaveNoIndices()) return false;
            string key = index.ToString(CultureInfo.InvariantCulture);
            if (prototype is null || !prototype.HasInProperty(key)) return false;
            value = prototype.GetWithReceiver(key, array); return true;
        }
        string name = index.ToString(CultureInfo.InvariantCulture);
        if (!source.HasInProperty(name)) return false;
        value = source.Get(name); return true;
    }
    internal static object IndexedValue(TsObject source, long index)
    {
        if (source is JsArray array)
        {
            if (array.HasElement((uint)index)) return array.ElementAt((uint)index);
            var prototype = array.PropertyPrototype;
            return prototype is null ? Undefined : prototype.GetWithReceiver(index.ToString(CultureInfo.InvariantCulture), array);
        }
        return source.Get(index.ToString(CultureInfo.InvariantCulture));
    }
    // Recheck each slot: callbacks may replace storage or install getters.
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    static bool TryCallbackNumber(TsObject source, long index, out double value)
    {
        if (source is NumberArray numbers && !numbers.HasIndexedAccessors && numbers.TryPacked(index + 1) is { } packed) {
            value = packed[(int)index]; return true;
        }
        if (source is TaggedArray tagged && index <= int.MaxValue && tagged.TryReadNumberAt((int)index, out var number) && number.HasValue) {
            value = number.Value; return true;
        }
        value = 0; return false;
    }
    enum ArrayCallbackKind { Map, Filter, ForEach, Every, Some, Find, FindIndex, FindLast, FindLastIndex }
    // Value-type kinds let the JIT specialize the shared loop for each method.
    interface IArrayCallbackKind { static abstract ArrayCallbackKind Value { get; } }
    readonly struct MapCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.Map; }
    readonly struct FilterCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.Filter; }
    readonly struct ForEachCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.ForEach; }
    readonly struct EveryCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.Every; }
    readonly struct SomeCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.Some; }
    readonly struct FindCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.Find; }
    readonly struct FindIndexCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.FindIndex; }
    readonly struct FindLastCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.FindLast; }
    readonly struct FindLastIndexCallbackKind : IArrayCallbackKind { public static ArrayCallbackKind Value => ArrayCallbackKind.FindLastIndex; }
    interface IArrayValueMode { static abstract bool Direct { get; } static abstract bool Number { get; } }
    readonly struct OrdinaryArrayValueMode : IArrayValueMode { public static bool Direct => false; public static bool Number => false; }
    readonly struct SingleArrayValueMode : IArrayValueMode { public static bool Direct => true; public static bool Number => false; }
    readonly struct NumberArrayValueMode : IArrayValueMode { public static bool Direct => true; public static bool Number => true; }
    sealed class ArrayCallbackFunction<TKind>(string name) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, name, 1)
        where TKind : struct, IArrayCallbackKind
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var callback = FunctionArgument(arguments, 0) as JsFunction;
            if (callback != null && callback.SupportsArrayValue() && !callback.SupportsArrayNumber() && !callback.SupportsArrayNumberSource())
                return callback.SupportsArrayValueNumber() ? Invoke<NumberArrayValueMode>(receiver, arguments)
                    : Invoke<SingleArrayValueMode>(receiver, arguments);
            return Invoke<OrdinaryArrayValueMode>(receiver, arguments);
        }
        static object Invoke<TValueMode>(object receiver, object[] arguments) where TValueMode : struct, IArrayValueMode
        {
            ArrayCallbackKind kind = TKind.Value;
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            if (FunctionArgument(arguments, 0) is not JsFunction callback) throw new InvalidOperationException("Array callback is not callable");
            object thisValue = FunctionArgument(arguments, 1);
            bool find = kind is ArrayCallbackKind.Find or ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLast or ArrayCallbackKind.FindLastIndex;
            bool backwards = kind is ArrayCallbackKind.FindLast or ArrayCallbackKind.FindLastIndex;
            bool numericCallback = callback.SupportsArrayNumber() || callback.SupportsArrayNumberSource();
            bool numericResult = numericCallback && callback.SupportsNumberResult();
            bool sourceNumberResult = numericCallback && callback.SupportsArrayNumberResult();
            bool unboxedResult = numericResult || sourceNumberResult;
            TsObject result = null;
            Span<object> mapSlots = default;
            bool privateResult = false;
            if (kind is ArrayCallbackKind.Map or ArrayCallbackKind.Filter) {
                result = ArraySpeciesCreate(source, kind == ArrayCallbackKind.Map ? length : 0, kind == ArrayCallbackKind.Map, out privateResult, kind == ArrayCallbackKind.Map && (unboxedResult || TValueMode.Number));
                // The intrinsic result cannot escape to callbacks. Its length,
                // storage and property attributes stay unchanged until return.
                if (kind == ArrayCallbackKind.Map && privateResult && result.GetType() == typeof(JsArray) && ((JsArray)result).DenseRange(length) is object[] slots) mapSlots = slots;
            }
            TaggedArray numericMap = kind == ArrayCallbackKind.Map && privateResult ? result as TaggedArray : null;
            bool testResult = kind is not ArrayCallbackKind.Map and not ArrayCallbackKind.ForEach;
            long selected = 0;
            for (long i = backwards ? length - 1 : 0; i >= 0 && i < length; i += backwards ? -1 : 1)
            {
                // Callbacks can replace storage, remove elements, or install
                // getters. Recheck the current element on every iteration.
                if (numericCallback && TryCallbackNumber(source, i, out double numberValue))
                {
                    object numericMapped;
                    bool truth;
                    if (unboxedResult)
                    {
                        double? mappedNumber = numericResult ? callback.InvokeNumber2(numberValue, i)
                            : callback.InvokeArrayNumberResult(thisValue, numberValue, i, source);
                        if (numericMap != null) { numericMap.StoreMappedNumber((int)i, mappedNumber); continue; }
                        numericMapped = kind == ArrayCallbackKind.Map ? BoxNumber(mappedNumber) : Undefined;
                        truth = testResult && NumberTruthy(mappedNumber);
                    }
                    else
                    {
                        numericMapped = callback.InvokeArrayNumber(thisValue, numberValue, i, source);
                        truth = testResult && Truthy(numericMapped);
                    }
                    if (find && truth) return kind is ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex ? (object)(double)i : BoxNumber(numberValue);
                    if (kind == ArrayCallbackKind.Map) { if (!mapSlots.IsEmpty) mapSlots[(int)i] = numericMapped; else ArraySpeciesSet(result, i, numericMapped); }
                    else if (kind == ArrayCallbackKind.Filter && truth) ArraySpeciesSet(result, selected++, BoxNumber(numberValue));
                    else if (kind == ArrayCallbackKind.Every && !truth) return BoxBoolean(false);
                    else if (kind == ArrayCallbackKind.Some && truth) return BoxBoolean(true);
                    continue;
                }
                if (!find && numericCallback && source is NumberArray sparse && !sparse.HasIndexedAccessors &&
                    !sparse.HasElement((uint)i) && ReferenceEquals(sparse.PropertyPrototype, ArrayPrototype) && ArraySlicePrototypesHaveNoIndices())
                {
                    // No user code runs while crossing holes. Recheck after every callback.
                    i = sparse.NextPresentIndex((uint)i, (uint)length);
                    if (i == length) break;
                }
                object value = null;
                if (find) value = IndexedValue(source, i);
                else if (!(TValueMode.Direct && i <= int.MaxValue && JsArray.TryReadOwnDenseValue(source, (int)i, out value)) &&
                    !TryIndexedValue(source, i, out value)) continue;
                object mapped;
                if (TValueMode.Number)
                {
                    double? mappedNumber = callback.InvokeArrayValueNumber(value);
                    if (numericMap != null) { numericMap.StoreMappedNumber((int)i, mappedNumber); continue; }
                    mapped = kind == ArrayCallbackKind.Map ? BoxNumber(mappedNumber)
                        : testResult ? BoxBoolean(NumberTruthy(mappedNumber)) : Undefined;
                }
                else if (unboxedResult && value is double sourceNumber)
                {
                    double? mappedNumber = numericResult ? callback.InvokeNumber2(sourceNumber, i)
                        : callback.InvokeArrayNumberResult(thisValue, sourceNumber, i, source);
                    if (numericMap != null) { numericMap.StoreMappedNumber((int)i, mappedNumber); continue; }
                    mapped = kind == ArrayCallbackKind.Map ? BoxNumber(mappedNumber)
                        : testResult ? BoxBoolean(NumberTruthy(mappedNumber)) : Undefined;
                }
                else mapped = numericCallback && value is double number
                    ? callback.InvokeArrayNumber(thisValue, number, i, source)
                    : TValueMode.Direct ? callback.InvokeContinuation(value)
                    : callback.InvokeWithReceiver(thisValue, new object[] { value, (double)i, source });
                if (find && Truthy(mapped)) return kind is ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex ? (object)(double)i : value;
                if (kind == ArrayCallbackKind.Map) { if (numericMap != null) numericMap.SetOwnElement((uint)i, mapped); else if (!mapSlots.IsEmpty) mapSlots[(int)i] = mapped; else ArraySpeciesSet(result, i, mapped); }
                else if (kind == ArrayCallbackKind.Filter) { if (Truthy(mapped)) ArraySpeciesSet(result, selected++, value); }
                else if (kind == ArrayCallbackKind.Every && !Truthy(mapped)) return BoxBoolean(false);
                else if (kind == ArrayCallbackKind.Some && Truthy(mapped)) return BoxBoolean(true);
            }
            return kind switch { ArrayCallbackKind.Every => BoxBoolean(true), ArrayCallbackKind.Some => BoxBoolean(false), ArrayCallbackKind.ForEach or ArrayCallbackKind.Find or ArrayCallbackKind.FindLast => Undefined, ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex => -1d, _ => result };
        }
    }
    sealed class ArrayReduceFunction(string name, bool backwards) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("reduce requires a receiver")), null, name, 1)
    {
        object Execute(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            if (FunctionArgument(arguments, 0) is not JsFunction callback) throw new InvalidOperationException("Array callback is not callable");
            bool initialized = arguments.Length > 1;
            bool numericCallback = callback.SupportsArrayNumber();
            bool numericResult = numericCallback && callback.SupportsNumberResult();
            bool valueNumberResult = callback.SupportsArrayReduceValueNumber();
            bool unboxed = false;
            double? numberAccumulator = null;
            object accumulator = FunctionArgument(arguments, 1);
            long step = backwards ? -1 : 1;
            for (long i = backwards ? length - 1 : 0; i >= 0 && i < length; i += step)
            {
                if (initialized && numericCallback && (unboxed || accumulator is double) &&
                    source is NumberArray numbers && !numbers.HasIndexedAccessors && numbers.TryPacked(i + 1) is { } packed)
                {
                    if (numericResult)
                    {
                        numberAccumulator = callback.InvokeNumber2(unboxed ? numberAccumulator : (double)accumulator, packed[(int)i]);
                        unboxed = true;
                    }
                    else accumulator = callback.InvokeArrayNumber(Undefined, (double)accumulator, packed[(int)i], source);
                    continue;
                }
                if (initialized && numericResult && (unboxed || accumulator is double) &&
                    source is TaggedArray tagged && i <= int.MaxValue && tagged.TryReadNumberAt((int)i, out var numberValue))
                {
                    numberAccumulator = callback.InvokeNumber2(unboxed ? numberAccumulator : (double)accumulator, numberValue);
                    unboxed = true;
                    continue;
                }
                object value = null;
                if (!(valueNumberResult && i <= int.MaxValue && JsArray.TryReadOwnDenseValue(source, (int)i, out value)) &&
                    !TryIndexedValue(source, i, out value)) continue;
                if (!initialized) { accumulator = value; initialized = true; }
                else if (numericResult && value is double number && (unboxed || accumulator is double))
                {
                    numberAccumulator = callback.InvokeNumber2(unboxed ? numberAccumulator : (double)accumulator, number);
                    unboxed = true;
                }
                else if (valueNumberResult && (unboxed || accumulator is double))
                {
                    numberAccumulator = callback.InvokeArrayReduceValueNumber(unboxed ? numberAccumulator : (double)accumulator, value);
                    unboxed = true;
                }
                else
                {
                    if (unboxed) { accumulator = BoxNumber(numberAccumulator); unboxed = false; }
                    // Eligible callbacks use only their first two numeric arguments.
                    if (numericCallback && accumulator is double first && value is double second)
                        accumulator = callback.InvokeArrayNumber(Undefined, first, second, source);
                    else accumulator = callback.InvokeWithReceiver(Undefined, new object[] { accumulator, value, (double)i, source });
                }
            }
            if (!initialized) throw new InvalidOperationException("Reduce of empty array with no initial value");
            return unboxed ? BoxNumber(numberAccumulator) : accumulator;
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, arguments);
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(Execute(receiver, Array.Empty<object>()), "Reduce result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => Cast<double?>(Execute(receiver, new[] { BoxNumber(first) }), "Reduce result violates number type");
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Cast<double?>(Execute(receiver, new[] { BoxNumber(first), BoxNumber(second) }), "Reduce result violates number type");
    }
}
