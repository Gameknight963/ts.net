using System;

public static partial class Js
{
    sealed class DisposableStackValue(bool asynchronous) : TsObject
    {
        internal readonly bool Asynchronous = asynchronous;
        internal bool Disposed;
        internal UsingResources Resources = new();
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)(Asynchronous ? RuntimeNamespaces.AsyncDisposableStack : RuntimeNamespaces.DisposableStack).Get("prototype");
        internal void CheckActive()
        {
            if (Disposed) throw MakeThrown(new JsCaughtError("ReferenceError", "Disposable stack has already been disposed"));
        }
    }
    static DisposableStackValue RequireDisposableStack(object receiver, bool asynchronous)
    {
        if (receiver is not DisposableStackValue stack || stack.Asynchronous != asynchronous)
            throw new InvalidOperationException("Method requires the matching disposable stack");
        return stack;
    }
    sealed class DisposableStackConstructor : NativeNamespaceFunction
    {
        readonly bool asynchronous;
        internal DisposableStackConstructor(bool asynchronous) : base(asynchronous ? "AsyncDisposableStack" : "DisposableStack", static _ => throw new InvalidOperationException("Disposable stack constructor requires new"), 0)
        {
            this.asynchronous = asynchronous;
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype}; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), asynchronous ? "AsyncDisposableStack" : "DisposableStack");
            prototype.DefineClassAccessor("disposed", new CollectionMethod("get disposed", 0, (receiver, _) => BoxBoolean(RequireDisposableStack(receiver, asynchronous).Disposed)), 1);
            prototype.DefineMethod("use", new CollectionMethod("use", 1, (receiver, args) => {
                var stack = RequireDisposableStack(receiver, asynchronous); stack.CheckActive();
                object value = FunctionArgument(args, 0);
                if (IsNullish(value)) return value;
                return asynchronous ? stack.Resources.AddAsync(value) : stack.Resources.Add(value);
            }));
            prototype.DefineMethod("adopt", new CollectionMethod("adopt", 2, (receiver, args) => {
                var stack = RequireDisposableStack(receiver, asynchronous); stack.CheckActive();
                object value = FunctionArgument(args, 0);
                if (FunctionArgument(args, 1) is not JsFunction callback) throw new InvalidOperationException("Disposal callback must be callable");
                var dispose = new JsFunction((FunctionBody)(_ => callback.InvokeWithReceiver(Undefined, new[] {value})), null, "", 0);
                stack.Resources.Defer(dispose, asynchronous); return value;
            }));
            prototype.DefineMethod("defer", new CollectionMethod("defer", 1, (receiver, args) => {
                var stack = RequireDisposableStack(receiver, asynchronous); stack.CheckActive();
                if (FunctionArgument(args, 0) is not JsFunction callback) throw new InvalidOperationException("Disposal callback must be callable");
                stack.Resources.Defer(callback, asynchronous); return Undefined;
            }));
            prototype.DefineMethod("move", new CollectionMethod("move", 0, (receiver, _) => {
                var stack = RequireDisposableStack(receiver, asynchronous); stack.CheckActive();
                var moved = new DisposableStackValue(asynchronous) {Resources = stack.Resources};
                stack.Resources = new UsingResources(); stack.Disposed = true; return moved;
            }));
            string name = asynchronous ? "disposeAsync" : "dispose";
            var disposal = new CollectionMethod(name, 0, (receiver, _) => {
                if (!asynchronous) {
                    var stack = RequireDisposableStack(receiver, false);
                    if (stack.Disposed) return Undefined;
                    stack.Disposed = true; return stack.Resources.Dispose();
                }
                try {
                    var stack = RequireDisposableStack(receiver, true);
                    if (!stack.Disposed) {stack.Disposed = true; return stack.Resources.DisposeAsync();}
                    var completed = new JsPromise(); completed.Settle(Undefined, false); return completed;
                } catch (Exception error) {
                    var rejected = new JsPromise(); rejected.Settle(CaughtValue(error), true); return rejected;
                }
            });
            prototype.DefineMethod(name, disposal);
            prototype.DefineMethod(PropertyKey(asynchronous ? AsyncDisposeSymbol : DisposeSymbol), disposal);
        }
        public override object Construct(object[] arguments) => new DisposableStackValue(asynchronous);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] arguments)
        {
            var stack = (TsObject)Construct(arguments); stack.SetPrototype(((TsObject)instance).PropertyPrototype); return stack;
        }
    }
}
