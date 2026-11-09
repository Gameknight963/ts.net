using System;

public static partial class Js
{
    enum PromiseCombination { All, AllSettled, Race, Any }
    sealed class PromiseCombinatorFunction(string name, PromiseCombination mode)
        : JsFunction((FunctionBody)(static _ => throw new InvalidOperationException("Promise method requires a constructor")), null, name, 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments)
        {
            // Creating the capability can throw synchronously. Everything after
            // that rejects the result, including a throwing resolve getter.
            var capability = NewPromiseCapability(receiver);
            object input = null;
            int position = 0; bool closeable = false;
            try
            {
                object resolve = Js.Get(receiver, "resolve");
                if (resolve is not JsFunction) throw new InvalidOperationException("Promise resolve is not callable");
                bool intrinsicResolve = ReferenceEquals(receiver, RuntimeNamespaces.Promise) && resolve is PromiseStaticFunction { IsResolve: true };
                input = Iteration.Start(FunctionArgument(arguments, 0));
                var state = new PromiseCombinationState(mode, capability);
                while (true)
                {
                    closeable = false;
                    if (position >= Iteration.Length(input)) break;
                    object item = input is JsArray array ? IndexedValue(array, position) : Iteration.Value(input, position);
                    position += Iteration.Step(input, position); closeable = true;
                    int index = state.Add();
                    object promise = intrinsicResolve ? PromiseResolve(receiver, item) : CallMethod(resolve, receiver, new[] { item });
                    object then = Js.Get(promise, "then");
                    if (promise is JsPromise native && then is PromiseThenFunction)
                    {
                        // Always read species; a custom constructor can observe
                        // the result even though the combinator discards it.
                        var species = PromiseSpecies(native);
                        var result = ReferenceEquals(species, RuntimeNamespaces.Promise) ? null : NewPromiseCapability(species);
                        native.Then(new PromiseReaction(result, null, null,
                            (value, rejected) => state.Accept(index, value, rejected), discardResult: true));
                        continue;
                    }
                    var handlers = state.Handlers(index);
                    CallMethod(then, promise, handlers);
                }
                state.Finish();
            }
            catch (Exception error) { if (closeable) CloseIterationAt(input, position, true); capability.Complete(CaughtValue(error), true); }
            return capability.Promise;
        }
    }
    sealed class PromiseCombinationState(PromiseCombination mode, PromiseCapability capability)
    {
        readonly JsArray values = new(0);
        int remaining = 1;
        internal int Add()
        {
            if (mode == PromiseCombination.Race) return 0;
            int index = checked((int)values.Length());
            values.Append(Undefined); remaining = checked(remaining + 1);
            return index;
        }
        internal void Accept(int index, object value, bool rejected)
        {
            if (mode == PromiseCombination.Race ||
                mode == PromiseCombination.All && rejected ||
                mode == PromiseCombination.Any && !rejected)
            {
                capability.Complete(value, rejected);
                return;
            }
            if (mode == PromiseCombination.AllSettled)
            {
                var record = new JsDynamicObject();
                record.DefineOwn("status", rejected ? "rejected" : "fulfilled");
                record.DefineOwn(rejected ? "reason" : "value", value);
                value = record;
            }
            values.SetAt(index, value);
            Finish();
        }
        internal object[] Handlers(int index)
        {
            if (mode == PromiseCombination.Race) return new object[] { capability.Resolve, capability.Reject };
            bool called = false;
            JsFunction Handler(bool rejected) => new((FunctionBody)(args =>
            {
                if (called) return Undefined;
                called = true;
                object value = FunctionArgument(args, 0);
                if (mode == PromiseCombination.AllSettled)
                {
                    var record = new JsDynamicObject();
                    record.DefineOwn("status", rejected ? "rejected" : "fulfilled");
                    record.DefineOwn(rejected ? "reason" : "value", value);
                    value = record;
                }
                values.SetAt(index, value);
                Finish();
                return Undefined;
            }), null, "", 1);
            return mode switch
            {
                PromiseCombination.All => new object[] { Handler(false), capability.Reject },
                PromiseCombination.Any => new object[] { capability.Resolve, Handler(true) },
                _ => new object[] { Handler(false), Handler(true) }
            };
        }
        internal void Finish()
        {
            if (mode == PromiseCombination.Race || --remaining != 0) return;
            if (mode == PromiseCombination.Any)
            {
                var error = new JsCaughtError("AggregateError", "All promises were rejected");
                error.DefineHidden("errors", values);
                capability.Complete(error, true);
            }
            else capability.Complete(values, false);
        }
    }
}
