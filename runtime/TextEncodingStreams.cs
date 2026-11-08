using System;
using System.Buffers;
using System.Text;

public static partial class Js
{
    sealed class StreamingTextEncoder
    {
        readonly Encoder encoder = Encoding.UTF8.GetEncoder();
        internal void Encode(object chunk, TransformController controller, bool flush)
        {
            string text = flush ? "" : StringValue(chunk);
            int capacity = Encoding.UTF8.GetMaxByteCount(text.Length);
            byte[] rented = null;
            Span<byte> buffer = capacity <= 512 ? stackalloc byte[capacity] : (rented = ArrayPool<byte>.Shared.Rent(capacity));
            try {
                int count = encoder.GetBytes(text.AsSpan(),buffer,flush);
                if (count == 0) return;
                var result = AllocateTypedArray("Uint8Array",count);
                buffer[..count].CopyTo(result.Buffer.Bytes);controller.Owner.Enqueue(result);
            }
            finally {if (rented != null) ArrayPool<byte>.Shared.Return(rented);}
        }
    }
    sealed class TextEncodingStreamValue : JsDescriptorObject
    {
        internal readonly bool Decoding;
        internal readonly TextDecoderValue Decoder;
        internal readonly TransformStreamValue Pipe;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)(Decoding ? TextEncodingStreams.Decoder : TextEncodingStreams.Encoder).Get("prototype");
        internal TextEncodingStreamValue(bool decoding, object[] args)
        {
            Decoding = decoding;
            var transformer = new JsClassInstance();
            if (decoding) {
                Decoder = NewTextDecoder(args);
                transformer.DefineOwn("transform",new JsFunction((FunctionBody)(values => {
                    object input = FunctionArgument(values,0);
                    if (input is not (BinaryBuffer or BinaryView or TypedArrayValue))
                        throw new InvalidOperationException("Decoder stream chunks must be valid buffers or views");
                    Emit(Decoder.Decode(EncodingInput(input),true),(TransformController)FunctionArgument(values,1));return Undefined;
                }),null,"transform",2));
                transformer.DefineOwn("flush",new JsFunction((FunctionBody)(values => {
                    Emit(Decoder.Decode(ReadOnlySpan<byte>.Empty,false),(TransformController)FunctionArgument(values,0));return Undefined;
                }),null,"flush",1));
            }
            else {
                var state = new StreamingTextEncoder();
                transformer.DefineOwn("transform",new JsFunction((FunctionBody)(values => {
                    state.Encode(FunctionArgument(values,0),(TransformController)FunctionArgument(values,1),false);return Undefined;
                }),null,"transform",2));
                transformer.DefineOwn("flush",new JsFunction((FunctionBody)(values => {
                    state.Encode(Undefined,(TransformController)FunctionArgument(values,0),true);return Undefined;
                }),null,"flush",1));
            }
            Pipe = new TransformStreamValue(transformer,Undefined,Undefined);
        }
        static void Emit(string text, TransformController controller) {if (text.Length != 0) controller.Owner.Enqueue(text);}
    }
    sealed class TextEncodingStreamConstructor : NativeNamespaceFunction
    {
        readonly bool decoding;
        internal TextEncodingStreamConstructor(bool decoding) : base(decoding ? "TextDecoderStream" : "TextEncoderStream",static _ => throw new InvalidOperationException("Constructor requires new"),0)
        {
            this.decoding = decoding;
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this);prototype.DefineMethod(PropertyKey(ToStringTagSymbol),decoding ? "TextDecoderStream" : "TextEncoderStream",false);
            TextEncodingStreamValue Require(object receiver) => receiver is TextEncodingStreamValue value && value.Decoding == decoding ? value : throw new InvalidOperationException("Text encoding stream receiver required");
            prototype.DefineAccessor("encoding",new CollectionMethod("get encoding",0,(receiver,_) => {var value = Require(receiver);return decoding ? value.Decoder.Name : "utf-8";}),1);
            prototype.DefineAccessor("readable",new CollectionMethod("get readable",0,(receiver,_) => Require(receiver).Pipe.Readable),1);
            prototype.DefineAccessor("writable",new CollectionMethod("get writable",0,(receiver,_) => Require(receiver).Pipe.Writable),1);
            if (decoding) {
                prototype.DefineAccessor("fatal",new CollectionMethod("get fatal",0,(receiver,_) => BoxBoolean(Require(receiver).Decoder.Fatal)),1);
                prototype.DefineAccessor("ignoreBOM",new CollectionMethod("get ignoreBOM",0,(receiver,_) => BoxBoolean(Require(receiver).Decoder.IgnoreBom)),1);
            }
        }
        public override object Construct(object[] args) => new TextEncodingStreamValue(decoding,args);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);var result = (TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class TextEncodingStreams
    {
        internal static readonly JsFunction Encoder = new TextEncodingStreamConstructor(false),Decoder = new TextEncodingStreamConstructor(true);
    }
}
