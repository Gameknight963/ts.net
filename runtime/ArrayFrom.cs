using System;
using System.Globalization;

public static partial class Js
{
    sealed class ArrayFromFunction() : JsFunction((FunctionBody)(static args => ArrayFrom(Undefined, args)), null, "from", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => ArrayFrom(receiver, arguments);
    }
    static object ArrayFrom(object receiver, object[] arguments)
    {
        object input = FunctionArgument(arguments, 0), mapper = FunctionArgument(arguments, 1), thisArg = FunctionArgument(arguments, 2);
        bool mapping = !IsUndefined(mapper);
        if (mapping && mapper is not JsFunction) throw new InvalidOperationException("Array.from mapper is not callable");
        object iterable = input;
        var iteratorMethod = CustomIterableMethod(iterable, out bool disabled);
        bool iterating = iteratorMethod != null || !disabled && (iterable is JsProtocolIterator or JsArray or JsArguments or string or JsKeyedCollection or JsCollectionIterator or JsSequenceIterator or JsRegexIterator || iterable is JsBoxedPrimitive { Value: string } || IsIteratorObject(iterable));
        TsObject source = iterating ? null : ToObject(input);
        long length = iterating ? 0 : IndexedLength(source);
        TsObject result;
        if (receiver is JsFunction constructor && constructor.CanInitializeDerived)
            result = (TsObject)constructor.Construct(iterating ? Array.Empty<object>() : new object[] { (double)length });
        else
        {
            var array = new JsArray(0);
            if (!iterating) array.SetLength(length);
            result = array;
        }
        // Packed numeric arrays have no holes or getters. With the intrinsic
        // constructor (or its non-constructor fallback), no user code can run
        // during the copy. Keep the result general so later writes can hold any value.
        bool intrinsic = receiver is not JsFunction function || !function.CanInitializeDerived || ReferenceEquals(receiver, RuntimeNamespaces.Array);
        if (!mapping && intrinsic && iteratorMethod == null && iterable is NumberArray numbers && numbers.Length() <= Array.MaxLength)
        {
            int packedCount = (int)numbers.Length();
            double[] packed = numbers.TryPacked(packedCount);
            if (packed != null)
            {
                var buffer = new object[packedCount];
                for (int index = 0; index < packedCount; index++) buffer[index] = packed[index];
                return new JsArray(buffer);
            }
        }
        object Map(object value, long index) => mapping ? CallMethod(mapper, thisArg, new object[] { value, (double)index }) : value;
        void Define(long index, object value)
        {
            if (result is JsArray array && index <= int.MaxValue) { array.SetAt((int)index, value); return; }
            string key = index.ToString(CultureInfo.InvariantCulture);
            if (result.IsModuleNamespace || result.IsReadOnlyOwnProperty(key)) throw new InvalidOperationException("Cannot define array item");
            if (result is JsArray wideArray) wideArray.Set(key, value);
            else result.DefineOwn(key, value);
        }
        long count = 0;
        if (iterating)
        {
            iterable = iteratorMethod != null ? StartIteratorObject(InvokeIterableMethod(iteratorMethod, iterable)) : Iteration.Start(iterable);
            int position = 0; bool closeable = false;
            try
            {
                // Array and arguments iterators observe length changes between steps.
                // String iteration advances by code point, not by UTF-16 code unit.
                while (true)
                {
                    closeable = false;
                    if (position >= Iteration.Length(iterable)) break;
                    object value = iterable is JsArray array ? IndexedValue(array, position) : Iteration.Value(iterable, position);
                    position += Iteration.Step(iterable, position); closeable = true;
                    Define(count, Map(value, count)); count++;
                }
            }
            catch { if (closeable) CloseIterationAt(iterable, position, true); throw; }
        }
        else
        {
            for (; count < length; count++) Define(count, Map(IndexedValue(source, count), count));
        }
        result.Set("length", (double)count);
        return result;
    }
}
