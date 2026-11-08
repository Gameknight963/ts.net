using System;
using System.Collections.Generic;

public static partial class Js
{
    sealed class FinalizationCell(object target, object held, object token)
    {
        internal readonly WeakReference<object> Target = new(target);
        internal readonly WeakReference<object> Token = IsUndefined(token) ? null : new(token);
        internal readonly object Held = held;
        internal bool Collected;
    }
    sealed class FinalizationRegistryValue(JsFunction callback) : JsDescriptorObject
    {
        readonly List<FinalizationCell> cells = new();
        bool queued;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)RuntimeNamespaces.FinalizationRegistry.Get("prototype");
        internal void Register(object target, object held, object token)
        {
            if (!CanBeWeakKey(target)) throw new InvalidOperationException("FinalizationRegistry target must be an object or non-registered symbol");
            if (SameValue(target, held)) throw new InvalidOperationException("FinalizationRegistry target and held value must differ");
            if (!IsUndefined(token) && !CanBeWeakKey(token)) throw new InvalidOperationException("FinalizationRegistry token must be an object or non-registered symbol");
            cells.Add(new(target, held, token));
        }
        internal bool Unregister(object token)
        {
            if (!CanBeWeakKey(token)) throw new InvalidOperationException("FinalizationRegistry token must be an object or non-registered symbol");
            return cells.RemoveAll(cell => cell.Token != null && cell.Token.TryGetTarget(out var candidate) && ReferenceEquals(candidate, token)) != 0;
        }
        internal void CheckTargets()
        {
            bool pending = false;
            foreach (var cell in cells) {
                if (!cell.Collected && !cell.Target.TryGetTarget(out _)) cell.Collected = true;
                pending |= cell.Collected;
            }
            if (!pending || queued) return;
            queued = true;
            var weak = new WeakReference<FinalizationRegistryValue>(this);
            var inbox = CaptureHostEvents();
            Action cancel = () => {
                if (weak.TryGetTarget(out var registry)) {
                    registry.queued = false;
                    RetryFinalizationCleanup(registry);
                }
            };
            inbox.Register(cancel);
            inbox.Post(() => {
                inbox.Unregister(cancel);
                if (weak.TryGetTarget(out var registry)) registry.Cleanup();
            });
        }
        void Cleanup()
        {
            queued = false;
            while (true) {
                int index = cells.FindIndex(cell => cell.Collected);
                if (index < 0) return;
                object held = cells[index].Held;
                cells.RemoveAt(index);
                try {callback.InvokeWithReceiver(Undefined, new[] {held});}
                catch {RetryFinalizationCleanup(this); throw;}
            }
        }
    }
    [ThreadStatic] static List<WeakReference<FinalizationRegistryValue>> finalizationRegistries;
    [ThreadStatic] static List<WeakReference<FinalizationRegistryValue>> finalizationRetries;
    static void RetryFinalizationCleanup(FinalizationRegistryValue registry) => (finalizationRetries ??= new()).Add(new(registry));
    [ThreadStatic] static int finalizationGcCount;
    static void CheckFinalizationRegistries()
    {
        var retries = finalizationRetries;
        finalizationRetries = null;
        if (retries != null)
            foreach (var retry in retries) if (retry.TryGetTarget(out var registry)) registry.CheckTargets();
        if (finalizationRegistries == null) return;
        int count = GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        if (count == finalizationGcCount) return;
        finalizationGcCount = count;
        for (int index = finalizationRegistries.Count - 1; index >= 0; index--) {
            if (finalizationRegistries[index].TryGetTarget(out var registry)) registry.CheckTargets();
            else finalizationRegistries.RemoveAt(index);
        }
    }
    sealed class FinalizationRegistryConstructor : NativeNamespaceFunction
    {
        internal FinalizationRegistryConstructor() : base("FinalizationRegistry", static _ => throw new InvalidOperationException("FinalizationRegistry requires new"), 1)
        {
            var prototype = new JsCreatedObject(); UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "FinalizationRegistry", false);
            prototype.DefineMethod("register", new CollectionMethod("register", 2, (receiver, args) => {
                if (receiver is not FinalizationRegistryValue registry) throw new InvalidOperationException("FinalizationRegistry receiver required");
                registry.Register(FunctionArgument(args, 0), FunctionArgument(args, 1), FunctionArgument(args, 2));
                return Undefined;
            }));
            prototype.DefineMethod("unregister", new CollectionMethod("unregister", 1, (receiver, args) => {
                if (receiver is not FinalizationRegistryValue registry) throw new InvalidOperationException("FinalizationRegistry receiver required");
                return BoxBoolean(registry.Unregister(FunctionArgument(args, 0)));
            }));
        }
        public override object Construct(object[] args)
        {
            if (FunctionArgument(args, 0) is not JsFunction callback) throw new InvalidOperationException("FinalizationRegistry callback must be callable");
            var registry = new FinalizationRegistryValue(callback);
            (finalizationRegistries ??= new()).Add(new(registry));
            return registry;
        }
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) {var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result;}
    }
}
