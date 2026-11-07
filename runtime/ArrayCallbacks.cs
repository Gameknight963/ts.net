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
            JsArray result = kind is ArrayCallbackKind.Map or ArrayCallbackKind.Filter ? new JsArray(0) : null;
            if (kind == ArrayCallbackKind.Map) result.SetLength(length);
            for (long i = backwards ? length - 1 : 0; i >= 0 && i < length; i += backwards ? -1 : 1)
            {
                object value;
                if (find) value = IndexedValue(source, i);
                else if (!TryIndexedValue(source, i, out value)) continue;
                object mapped = callback.InvokeWithReceiver(thisValue, new object[] { value, (double)i, source });
                if (find && Truthy(mapped)) return kind is ArrayCallbackKind.FindIndex or ArrayCallbackKind.FindLastIndex ? (object)(double)i : value;
                if (kind == ArrayCallbackKind.Map) result.Set(i.ToString(CultureInfo.InvariantCulture), mapped);
                else if (kind == ArrayCallbackKind.Filter) { if (Truthy(mapped)) result.Append(value); }
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
            object accumulator = FunctionArgument(arguments, 1);
            long step = backwards ? -1 : 1;
            for (long i = backwards ? length - 1 : 0; i >= 0 && i < length; i += step)
            {
                if (!TryIndexedValue(source, i, out object value)) continue;
                if (!initialized) { accumulator = value; initialized = true; }
                else accumulator = callback.InvokeWithReceiver(Undefined, new object[] { accumulator, value, (double)i, source });
            }
            return initialized ? accumulator : throw new InvalidOperationException("Reduce of empty array with no initial value");
        }
        public override object InvokeWithReceiver(object receiver, object[] arguments) => Execute(receiver, arguments);
        public override double? InvokeMethodNumber0(object receiver) => Cast<double?>(Execute(receiver, Array.Empty<object>()), "Reduce result violates number type");
        public override double? InvokeMethodNumber1(object receiver, double? first) => Cast<double?>(Execute(receiver, new[] { BoxNumber(first) }), "Reduce result violates number type");
        public override double? InvokeMethodNumber2(object receiver, double? first, double? second) => Cast<double?>(Execute(receiver, new[] { BoxNumber(first), BoxNumber(second) }), "Reduce result violates number type");
    }
}
