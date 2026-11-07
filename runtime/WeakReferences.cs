using System;
using System.Collections.Generic;

internal sealed class JsWeakRef(TsObject target) : TsObject
{
    internal readonly WeakReference<TsObject> Target = new(target);
    internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : Js.WeakRefPrototype;
}

public static partial class Js
{
    [ThreadStatic] static HashSet<TsObject> keptWeakTargets;
    internal static TsObject WeakRefPrototype => (TsObject)RuntimeNamespaces.WeakRef.Get("prototype");
    static void KeepWeakTarget(TsObject target) => (keptWeakTargets ??= new(ReferenceEqualityComparer.Instance)).Add(target);
    static void ClearKeptWeakTargets() => keptWeakTargets?.Clear();
    static JsWeakRef CreateWeakRef(object[] arguments)
    {
        if (FunctionArgument(arguments, 0) is not TsObject target) throw new InvalidOperationException("WeakRef target must be an object");
        var result = new JsWeakRef(target); KeepWeakTarget(target); return result;
    }
    sealed class WeakRefConstructor : NativeNamespaceFunction
    {
        internal WeakRefConstructor() : base("WeakRef", static _ => throw new InvalidOperationException("WeakRef constructor requires new"), 1)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod("deref", new CollectionMethod("deref", 0, (receiver, _) =>
            {
                if (receiver is not JsWeakRef reference) throw new InvalidOperationException("WeakRef deref requires a weak reference");
                if (!reference.Target.TryGetTarget(out var target)) return Undefined;
                KeepWeakTarget(target); return target;
            }));
        }
        public override object Construct(object[] arguments) => CreateWeakRef(arguments);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments) { var result = CreateWeakRef(arguments); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
