using System;
using System.IO;

public static partial class Js
{
    static JsPromise ConsumeStreamBody(CustomReadableStream stream, HeadersValue headers, string kind)
    {
        if (stream.Disturbed || StreamLocked(stream)) throw new InvalidOperationException("Body has already been consumed or locked");
        var reader = stream.GetReader(Undefined);
        var bytes = new MemoryStream();
        var result = new JsPromise();
        void Finish(object value, bool rejected)
        {
            bytes.Dispose();
            // Body consumption keeps the reader's lock, including on errors.
            result.Settle(value,rejected);
        }
        void Read()
        {
            reader.Read().Then(new PromiseContinuationReaction((value,rejected) => {
                if (rejected) {Finish(value,true);return;}
                try {
                    var item = (TsObject)value;
                    if (Truthy(item.Get("done"))) {Finish(ConvertHttpBody(bytes.ToArray(),headers,kind),false);return;}
                    if (item.Get("value") is not TypedArrayValue {Name:"Uint8Array"} chunk) throw new InvalidOperationException("Body stream chunks must be Uint8Array");
                    bytes.Write(EncodingInput(chunk));
                    Read();
                }
                catch (Exception error) {Finish(CaughtValue(error),true);}
            }));
        }
        Read();
        return result;
    }
}
