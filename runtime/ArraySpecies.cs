using System;
using System.Globalization;

public static partial class Js
{
    sealed class ConstructorSpeciesGetter() : JsFunction((FunctionBody)(static _ => Undefined), null, "get [Symbol.species]", 0)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => receiver;
    }
    [ThreadStatic] static int objectPrototypeIndexVersion;
    [ThreadStatic] static bool objectPrototypeIndicesChecked, objectPrototypeHasIndices;
    internal static bool ArraySlicePrototypesHaveNoIndices()
    {
        if (!ReferenceEquals(ArrayPrototype.PropertyPrototype, DefaultClassObjectPrototype) || DefaultClassObjectPrototype.PropertyPrototype != null || ((JsArray)ArrayPrototype).Length() != 0) return false;
        int version = DefaultClassObjectPrototype.OwnPropertyVersion;
        if (!objectPrototypeIndicesChecked || version != objectPrototypeIndexVersion) {
            var names = (JsArray)DefaultClassObjectPrototype.OwnPropertyNames();
            bool found = false;
            for (uint i = 0; i < names.Length(); i++) if (JsArray.Index((string)names.ElementAt(i), out _)) { found = true; break; }
            objectPrototypeHasIndices = found;
            objectPrototypeIndexVersion = version;
            objectPrototypeIndicesChecked = true;
        }
        return !objectPrototypeHasIndices;
    }
    static JsFunction ArraySpeciesConstructor(TsObject source, out bool native)
    {
        native = false;
        if (source is not JsArray) return null;
        string key = PropertyKey(SpeciesSymbol);
        // This guard avoids calling user code on the existing array-copy path.
        if (!source.HasOwn("constructor") && ReferenceEquals(source.PropertyPrototype, ArrayPrototype) &&
            !ArrayPrototype.TryOwnAccessor("constructor", out _) && ReferenceEquals(ArrayPrototype.Get("constructor"), RuntimeNamespaces.Array) &&
            RuntimeNamespaces.Array.TryOwnAccessor(key, out var accessor) && accessor.Getter is ConstructorSpeciesGetter)
        { native = true; return null; }
        object constructor = source.Get("constructor");
        if (IsUndefined(constructor)) return null;
        if (constructor is not TsObject obj) throw new InvalidOperationException("Array constructor must be an object");
        object species = obj.Get(key);
        if (IsNullish(species)) return null;
        if (species is not JsFunction function || !function.CanInitializeDerived) throw new InvalidOperationException("Array species must be a constructor");
        return ReferenceEquals(function, RuntimeNamespaces.Array) ? null : function;
    }

    static TsObject ArraySpeciesCreate(TsObject source, long length) => ArraySpeciesCreate(ArraySpeciesConstructor(source, out _), length);
    static TsObject ArraySpeciesCreate(JsFunction constructor, long length)
    {
        if (constructor != null) return constructor.Construct(new object[] { (double)length }) as TsObject ?? throw new InvalidOperationException("Array species returned a primitive");
        if (length > uint.MaxValue) throw new ArgumentOutOfRangeException(null, "Invalid array length");
        var result = new JsArray(0); result.SetLength(length); return result;
    }
    static void ArraySpeciesSet(TsObject destination, long index, object value)
    {
        // Creating an own property ignores inherited setters. Sealed elements
        // still need descriptor validation because the new property is configurable.
        if (destination.GetType() == typeof(JsArray) && !((JsArray)destination).ElementsSealed && !destination.HasPropertyOverrides && !destination.IsModuleNamespace && index <= int.MaxValue)
        { ((JsArray)destination).SetOwnElement((uint)index, value); return; }
        string key = index.ToString(CultureInfo.InvariantCulture);
        if (destination is JsArray or JsDescriptorObject) {
            ApplyPropertyDefinition(destination, key, new(value, null, null, true, true, true,
                DescriptorFields.Value | DescriptorFields.Writable | DescriptorFields.Enumerable | DescriptorFields.Configurable));
            return;
        }
        if (destination.IsModuleNamespace) throw new InvalidOperationException("Cannot define array result item");
        if (destination.HasOwn(key))
        {
            var descriptor = (TsObject)destination.OwnPropertyDescriptor(key);
            if (!Truthy(descriptor.Get("configurable"))) throw new InvalidOperationException("Cannot redefine array result item");
            if (!descriptor.HasOwn("writable") || !Truthy(descriptor.Get("writable")) || !Truthy(descriptor.Get("enumerable")))
                if (!destination.DeleteOwn(key)) throw new InvalidOperationException("Cannot redefine array result item");
        }
        if (destination is TypedArrayValue typed && index >= typed.Count) throw new InvalidOperationException("Array result index is out of range");
        if (destination is JsArray array && index <= int.MaxValue) array.SetAt((int)index, value); else destination.SetOwnValue(key, value);
    }
}
