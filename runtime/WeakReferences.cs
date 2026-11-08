using System;
using System.Collections.Generic;

internal sealed class JsWeakRef(object target) : JsDescriptorObject
{
    internal readonly WeakReference<object> Target = new(target);
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.WeakRefPrototype;
}

public static partial class Js
{
    [ThreadStatic] static HashSet<object> keptWeakTargets;
    internal static TsObject WeakRefPrototype => (TsObject)RuntimeNamespaces.WeakRef.Get("prototype");
    static void KeepWeakTarget(object target) => (keptWeakTargets ??= new(ReferenceEqualityComparer.Instance)).Add(target);
    static void ClearKeptWeakTargets() => keptWeakTargets?.Clear();
    static object WeakRefTarget(object[] arguments)
    {
        object target = FunctionArgument(arguments, 0);
        if (!CanBeWeakKey(target)) throw new InvalidOperationException("WeakRef target must be an object or non-registered symbol");
        return target;
    }
    static JsWeakRef CreateWeakRef(object target)
    {
        var result = new JsWeakRef(target); KeepWeakTarget(target); return result;
    }
    sealed class WeakRefConstructor : NativeNamespaceFunction
    {
        internal WeakRefConstructor() : base("WeakRef", static _ => throw new InvalidOperationException("WeakRef constructor requires new"), 1)
        {
            var prototype = new JsCreatedObject(); UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "WeakRef", false);
            prototype.DefineMethod("deref", new CollectionMethod("deref", 0, (receiver, _) =>
            {
                if (receiver is not JsWeakRef reference) throw new InvalidOperationException("WeakRef deref requires a weak reference");
                if (!reference.Target.TryGetTarget(out var target)) return Undefined;
                KeepWeakTarget(target); return target;
            }));
        }
        public override object Construct(object[] arguments) => CreateWeakRef(WeakRefTarget(arguments));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments, JsFunction newTarget)
        {
            object target = WeakRefTarget(arguments);
            var prototype = ConstructionPrototype(newTarget);
            var result = CreateWeakRef(target);
            result.SetPrototype(prototype);
            return result;
        }
        internal override object InitializeDerived(object instance, object[] arguments) { var result = CreateWeakRef(WeakRefTarget(arguments)); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
