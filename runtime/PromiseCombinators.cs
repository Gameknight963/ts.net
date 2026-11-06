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
            try
            {
                object resolve = Js.Get(receiver, "resolve");
                if (resolve is not JsFunction) throw new InvalidOperationException("Promise resolve is not callable");
                input = Iteration.Start(FunctionArgument(arguments, 0));
                var state = new PromiseCombinationState(mode, capability);
                for (int position = 0; position < Iteration.Length(input); position += Iteration.Step(input, position))
                {
                    object item = input is JsArray array ? IndexedValue(array, position) : Iteration.Value(input, position);
                    int index = state.Add();
                    object promise = CallMethod(resolve, receiver, new[] { item });
                    var handlers = state.Handlers(index);
                    CallMethod(Js.Get(promise, "then"), promise, handlers);
                }
                state.Finish();
            }
            catch (Exception error) { CloseIteration(input, true); capability.Complete(CaughtValue(error), true); }
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
