using System;
using System.IO;

public static partial class Js
{
    static JsPromise ConsumeStreamBody(CustomReadableStream stream, HeadersValue headers, string kind)
    {
        if (stream.Disturbed || StreamLocked(stream)) throw new InvalidOperationException("Body has already been consumed or locked");
        var reader = stream.GetReader(Undefined);
        var operation = new StreamBodyConsumption(reader, headers, kind);
        operation.Read();
        return operation.Result;
    }
    sealed class StreamBodyConsumption(CustomStreamReader reader, HeadersValue headers, string kind) : PromiseReactionBase
    {
        readonly MemoryStream bytes = new();
        internal readonly JsPromise Result = new();
        void Finish(object value, bool rejected)
        {
            bytes.Dispose();
            // Body consumption keeps the reader's lock, including on errors.
            Result.Settle(value,rejected);
        }
        // Reads settle through the microtask queue, so only one attachment is
        // outstanding and the same reaction can consume every chunk.
        internal void Read() => reader.Read().Then(this);
        internal override void Run(object value, bool rejected)
        {
            if (rejected) {Finish(value,true);return;}
            try {
                var item = (TsObject)value;
                if (Truthy(item.Get("done"))) {Finish(ConvertHttpBody(bytes.ToArray(),headers,kind),false);return;}
                if (item.Get("value") is not TypedArrayValue {Name:"Uint8Array"} chunk) throw new InvalidOperationException("Body stream chunks must be Uint8Array");
                bytes.Write(EncodingInput(chunk));
                Read();
            }
            catch (Exception error) {Finish(CaughtValue(error),true);}
        }
    }
}
