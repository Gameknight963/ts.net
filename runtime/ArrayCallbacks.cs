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
    enum ArrayCallbackKind { Map, Filter, ForEach, Every, Some, Find, FindIndex, FindLast, FindLastIndex }
    sealed class ArrayCallbackFunction(string name, ArrayCallbackKind kind) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, name, 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            long length = IndexedLength(source);
            if (FunctionArgument(arguments, 0) is not JsFunction callback) throw new InvalidOperationException("Array callback is not callable");
            object thisValue = FunctionArgument(arguments, 1);
            bool find = kind is ArrayCallbackKind.Find or ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLast or ArrayCallbackKind.FindLastIndex;
            bool backwards = kind is ArrayCallbackKind.FindLast or ArrayCallbackKind.FindLastIndex;
            TsObject result = kind is ArrayCallbackKind.Map or ArrayCallbackKind.Filter ? ArraySpeciesCreate(source, kind == ArrayCallbackKind.Map ? length : 0) : null;
            bool numericCallback = callback.SupportsArrayNumber();
            long selected = 0;
            for (long i = backwards ? length - 1 : 0; i >= 0 && i < length; i += backwards ? -1 : 1)
            {
                // Callbacks can replace storage, remove elements, or install
                // getters. Recheck the current element on every iteration.
                if (numericCallback && (kind is ArrayCallbackKind.Map or ArrayCallbackKind.Filter) &&
                    source is NumberArray numbers && !numbers.HasIndexedAccessors && numbers.TryPacked(i + 1) is { } packed)
                {
                    double numberValue = packed[(int)i];
                    object numericMapped = callback.InvokeArrayNumber(thisValue, numberValue, i, source);
                    if (kind == ArrayCallbackKind.Map) ArraySpeciesSet(result, i, numericMapped);
                    else if (Truthy(numericMapped)) ArraySpeciesSet(result, selected++, BoxNumber(numberValue));
                    continue;
                }
                object value;
                if (find) value = IndexedValue(source, i);
                else if (!TryIndexedValue(source, i, out value)) continue;
                object mapped = numericCallback && value is double number
                    ? callback.InvokeArrayNumber(thisValue, number, i, source)
                    : callback.InvokeWithReceiver(thisValue, new object[] { value, (double)i, source });
                if (find && Truthy(mapped)) return kind is ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex ? (object)(double)i : value;
                if (kind == ArrayCallbackKind.Map) ArraySpeciesSet(result, i, mapped);
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
                if (!TryIndexedValue(source, i, out object value)) continue;
                if (!initialized) { accumulator = value; initialized = true; }
                else if (numericResult && value is double number && (unboxed || accumulator is double))
                {
                    numberAccumulator = callback.InvokeNumber2(unboxed ? numberAccumulator : (double)accumulator, number);
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
