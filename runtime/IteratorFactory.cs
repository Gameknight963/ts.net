using System;

internal sealed class JsIteratorWrapper(TsObject source, object next) : TsObject
{
    internal readonly TsObject Source = source;
    internal readonly object NextMethod = next;
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.IteratorWrapperPrototype;
}

public static partial class Js
{
    internal static bool IsIteratorObject(object value)
    {
        if (value is not TsObject obj) return false;
        for (var prototype = obj.PropertyPrototype; prototype != null; prototype = prototype.PropertyPrototype)
            if (ReferenceEquals(prototype, NativeIteratorPrototype)) return true;
        return false;
    }
    internal static object StartIteratorObject(TsObject source)
    {
        return new JsProtocolIterator(source, source.Get("next"));
    }
    static class IteratorWrapperMethods
    {
        internal static readonly TsObject Prototype = Create();
        static TsObject Create()
        {
            var prototype = new JsClassInstance { ParentPrototype = NativeIteratorPrototype };
            prototype.DefineMethod("next", new CollectionMethod("next", 0, (receiver, _) => receiver is JsIteratorWrapper wrapper ? CallMethod(wrapper.NextMethod, wrapper.Source, Array.Empty<object>()) : throw new InvalidOperationException("Incompatible iterator receiver")));
            prototype.DefineMethod("return", new CollectionMethod("return", 0, (receiver, _) =>
            {
                if (receiver is not JsIteratorWrapper wrapper) throw new InvalidOperationException("Incompatible iterator receiver");
                object method = wrapper.Source.Get("return");
                return IsNullish(method) ? IteratorResult(Undefined, true) : CallMethod(method, wrapper.Source, Array.Empty<object>());
            }));
            return prototype;
        }
    }
    internal static TsObject IteratorWrapperPrototype => IteratorWrapperMethods.Prototype;
    static object IteratorFrom(object value)
    {
        TsObject source = TryGetIterableObject(value, out var custom) ? custom : value switch
        {
            string text => new JsSequenceIterator(text, 0u),
            JsBoxedPrimitive { Value: string text } => new JsSequenceIterator(text, 0u),
            JsArray or JsArguments => new JsSequenceIterator(value, 0u),
            JsKeyedCollection collection => new JsCollectionIterator(collection, collection.IsSet ? 1 : 2),
            TsObject obj => obj,
            _ => throw new InvalidOperationException("Iterator.from requires an object or string")
        };
        object next = source.Get("next");
        return IsIteratorObject(source) ? source : new JsIteratorWrapper(source, next);
    }
    sealed class IteratorConstructor : NativeNamespaceFunction
    {
        internal IteratorConstructor() : base("Iterator", static _ => throw new InvalidOperationException("Iterator constructor requires new"), 0)
        {
            var prototype = (JsClassInstance)NativeIteratorPrototype; UseObjectPrototype(prototype); prototype.DefineMethod("constructor", this);
            DefineIteratorTerminals(prototype);
            DefineIteratorLazy(prototype);
            Member("from", new JsFunction((FunctionBody)(static arguments => IteratorFrom(FunctionArgument(arguments, 0))), null, "from", 1));
            Member("concat", new JsFunction((FunctionBody)IteratorConcat, null, "concat", 0));
        }
        public override object Construct(object[] arguments) => throw new InvalidOperationException("Iterator is an abstract constructor");
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments) => instance;
    }
}
