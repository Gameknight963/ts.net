using System;
using System.Globalization;

public static partial class Js
{
    enum ArrayCallbackKind { Map, Filter, ForEach, Every, Some }
    sealed class ArrayCallbackFunction(string name, ArrayCallbackKind kind) : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Array method requires a receiver")), null, name, 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            var source = ToObject(receiver);
            double count = Number(source.Get("length"));
            long length = double.IsNaN(count) || count <= 0 ? 0 : (long)Math.Min(9007199254740991d, Math.Floor(count));
            if (FunctionArgument(arguments, 0) is not JsFunction callback) throw new InvalidOperationException("Array callback is not callable");
            object thisValue = FunctionArgument(arguments, 1);
            JsArray result = kind is ArrayCallbackKind.Map or ArrayCallbackKind.Filter ? new JsArray(0) : null;
            if (kind == ArrayCallbackKind.Map) result.SetLength(length);
            for (long i = 0; i < length; i++)
            {
                string key = i.ToString(CultureInfo.InvariantCulture);
                object value;
                if (source is JsArray array)
                {
                    uint index = (uint)i;
                    if (array.HasElement(index)) value = array.ElementAt(index);
                    else
                    {
                        var prototype = array.PropertyPrototype;
                        if (prototype is null || !prototype.HasInProperty(key)) continue;
                        value = prototype.GetWithReceiver(key, array);
                    }
                }
                else { if (!source.HasInProperty(key)) continue; value = source.Get(key); }
                object mapped = callback.InvokeWithReceiver(thisValue, new object[] { value, (double)i, source });
                if (kind == ArrayCallbackKind.Map) result.Set(key, mapped);
                else if (kind == ArrayCallbackKind.Filter) { if (Truthy(mapped)) result.Append(value); }
                else if (kind == ArrayCallbackKind.Every && !Truthy(mapped)) return BoxBoolean(false);
                else if (kind == ArrayCallbackKind.Some && Truthy(mapped)) return BoxBoolean(true);
            }
            return kind switch { ArrayCallbackKind.Every => BoxBoolean(true), ArrayCallbackKind.Some => BoxBoolean(false), ArrayCallbackKind.ForEach => Undefined, _ => result };
        }
    }
}
