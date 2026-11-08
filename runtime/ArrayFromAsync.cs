using System;

public static partial class Js
{
    sealed class ArrayFromAsyncFunction() : JsFunction((FunctionBody)(static arguments => FromAsync(Undefined, arguments)), null, "fromAsync", 1)
    {
        public override object InvokeWithReceiver(object receiver, object[] arguments) => FromAsync(receiver, arguments);
    }
    static object FromAsync(object receiver, object[] arguments)
    {
        var state = new ArrayFromAsyncState(); state.Start(receiver, arguments); return state.Promise;
    }
    sealed class ArrayFromAsyncState
    {
        internal readonly JsPromise Promise = new();
        object iterator, mapper, thisArg;
        AsyncIterationState asyncCursor;
        TsObject source, target;
        long length, count;
        int position;
        bool iterating, closeable, finished;
        internal void Start(object receiver, object[] arguments)
        {
            try
            {
                object input = FunctionArgument(arguments, 0);
                mapper = FunctionArgument(arguments, 1); thisArg = FunctionArgument(arguments, 2);
                if (!IsUndefined(mapper) && mapper is not JsFunction) throw new InvalidOperationException("Array.fromAsync mapper is not callable");
                object iterable = input;
                bool disabled = false;
                if (TryGetAsyncIterator(iterable, out var asyncSource, out var asyncNext))
                    asyncCursor = new AsyncIterationState { Source = asyncSource, AsyncNext = asyncNext, Asynchronous = true, Started = true };
                else {
                    var method = CustomIterableMethod(iterable, out disabled);
                    if (method != null) iterable = StartIteratorObject(InvokeIterableMethod(method, iterable));
                }
                iterating = asyncCursor != null || !disabled && (iterable is JsProtocolIterator or JsArray or JsArguments or string or JsKeyedCollection or JsCollectionIterator or JsSequenceIterator or JsRegexIterator || iterable is JsBoxedPrimitive { Value: string } || IsIteratorObject(iterable));
                if (iterating) { if (asyncCursor == null) iterator = Iteration.Start(iterable); }
                else { source = ToObject(input); length = IndexedLength(source); }
                target = receiver is JsFunction constructor && constructor.CanInitializeDerived
                    ? (TsObject)constructor.Construct(iterating ? Array.Empty<object>() : new object[] { (double)length }) : new JsArray(0);
                Step();
            }
            catch (Exception error) { Fail(CaughtValue(error)); }
        }
        void Fail(object reason)
        {
            if (finished) return; finished = true;
            void Reject() { iterator = source = target = null; asyncCursor = null; Promise.Settle(reason, true); }
            if (closeable)
            {
                try
                {
                    object closed = ForAwaitClose(asyncCursor ?? new AsyncIterationState { Source = iterator, Position = (uint)position });
                    ((JsPromise)PromiseResolve(RuntimeNamespaces.Promise, closed)).Then(new PromiseReaction(null, null, null, (_, _) => Reject()));
                }
                catch { Reject(); }
            }
            else Reject();
        }
        void Await(object value, Action<object> continuation)
        {
            var promise = (JsPromise)PromiseResolve(RuntimeNamespaces.Promise, value);
            promise.Then(new PromiseReaction(null, null, null, (resolved, rejected) =>
            {
                if (rejected) { Fail(resolved); return; }
                try { continuation(resolved); } catch (Exception error) { Fail(CaughtValue(error)); }
            }));
        }
        void Step()
        {
            closeable = false;
            if (asyncCursor != null) {
                Await(ForAwaitNext(asyncCursor), result => {
                    if (!ForAwaitAccept(asyncCursor, result)) { Complete(); return; }
                    closeable = true;
                    Accepted(AsyncIterationValue(asyncCursor));
                });
                return;
            }
            if (iterating ? position >= Iteration.Length(iterator) : count >= length)
            {
                Complete(); return;
            }
            object value;
            if (iterating)
            {
                value = iterator is JsArray array ? IndexedValue(array, position) : Iteration.Value(iterator, position);
                position += Iteration.Step(iterator, position); closeable = true;
            }
            else value = IndexedValue(source, count);
            Await(value, Accepted);
        }
        void Complete()
        {
            target.Set("length", (double)count); finished = true;
            var result = target; iterator = source = target = null; asyncCursor = null; Promise.ResolveValue(result);
        }
        void Accepted(object value)
        {
            if (mapper is JsFunction function) Await(function.InvokeWithReceiver(thisArg, new object[] { value, (double)count }), Store);
            else Store(value);
        }
        void Store(object value)
        {
            ArraySpeciesSet(target, count, value);
            count++; Step();
        }
    }
}
