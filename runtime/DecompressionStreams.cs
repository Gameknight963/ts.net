using System;
using System.Buffers;
using System.IO.Compression;

public static partial class Js
{
    sealed class DecompressionState : IDisposable
    {
        readonly NativeInflate inflate;
        BrotliDecoder brotli;
        readonly bool isBrotli;
        byte[] output = ArrayPool<byte>.Shared.Rent(16384);
        bool finished,disposed;
        internal DecompressionState(string format)
        {
            if (format is not ("gzip" or "deflate" or "deflate-raw" or "brotli")) {Dispose();throw new InvalidOperationException("Unsupported decompression format");}
            isBrotli = format == "brotli";
            try {if (!isBrotli) inflate = new NativeInflate(format);}
            catch {Dispose();throw;}
        }
        internal void Write(object chunk,TransformController controller)
        {
            try {
                if (chunk is not (BinaryBuffer or BinaryView or TypedArrayValue)) throw new InvalidOperationException("Decompression chunks must be buffers or views");
                // BufferSource input is snapshotted before a decoder retains it.
                Decode(EncodingInput(chunk).ToArray(),controller);
            }
            catch {Dispose();throw;}
        }
        void Decode(byte[] input,TransformController controller)
        {
            if (finished) {if (input.Length != 0) throw new InvalidOperationException("Unexpected bytes after the compressed stream");return;}
            int offset = 0;
            while (true) {
                int status,consumed,written;
                if (isBrotli) {
                    var result = brotli.Decompress(input.AsSpan(offset),output,out consumed,out written);
                    status = result == OperationStatus.Done ? 1 : result == OperationStatus.InvalidData ? -3 : 0;
                }
                else status = inflate.Decode(input,offset,input.Length-offset,output,out consumed,out written);
                offset += consumed;
                if (status is not (0 or 1 or -5)) throw new InvalidOperationException("Compressed data is invalid");
                if (written != 0) {
                    var chunk = AllocateTypedArray("Uint8Array",written);output.AsSpan(0,written).CopyTo(chunk.Buffer.Bytes);controller.Owner.Enqueue(chunk);
                }
                if (status == 1) {
                    finished = true;
                    if (offset != input.Length) throw new InvalidOperationException("Unexpected bytes after the compressed stream");
                    Dispose();return;
                }
                if (consumed == 0 && written == 0) {
                    if (offset != input.Length) throw new InvalidOperationException("Decompression made no progress");
                    return;
                }
                if (offset == input.Length && written < output.Length) return;
            }
        }
        internal void Finish(TransformController controller)
        {
            try {
                if (!finished) Decode(Array.Empty<byte>(),controller);
                if (!finished) throw new InvalidOperationException("Compressed input ended before the stream was complete");
            }
            finally {Dispose();}
        }
        public void Dispose()
        {
            if (disposed) return;disposed = true;
            inflate?.Dispose();brotli.Dispose();
            var buffer = output;output = null;if (buffer != null) ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    sealed class DecompressionStreamValue : JsDescriptorObject
    {
        internal readonly TransformStreamValue Pipe;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)DecompressionStreams.Constructor.Get("prototype");
        internal DecompressionStreamValue(string format)
        {
            var state = new DecompressionState(format);var transformer = new JsClassInstance();
            transformer.DefineOwn("transform",new JsFunction((FunctionBody)(args => {state.Write(FunctionArgument(args,0),(TransformController)FunctionArgument(args,1));return Undefined;}),null,"transform",2));
            transformer.DefineOwn("flush",new JsFunction((FunctionBody)(args => {state.Finish((TransformController)FunctionArgument(args,0));return Undefined;}),null,"flush",1));
            transformer.DefineOwn("cancel",new JsFunction((FunctionBody)(_ => {state.Dispose();return Undefined;}),null,"cancel",1));
            try {Pipe = new TransformStreamValue(transformer,Undefined,Undefined);}catch {state.Dispose();throw;}
        }
    }
    sealed class DecompressionConstructor : NativeNamespaceFunction
    {
        internal DecompressionConstructor() : base("DecompressionStream",static _ => throw new InvalidOperationException("DecompressionStream requires new"),1)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this);prototype.DefineMethod(PropertyKey(ToStringTagSymbol),"DecompressionStream",false);
            DecompressionStreamValue Require(object receiver) => receiver as DecompressionStreamValue ?? throw new InvalidOperationException("Decompression stream receiver required");
            prototype.DefineAccessor("readable",new CollectionMethod("get readable",0,(receiver,_) => Require(receiver).Pipe.Readable),1);
            prototype.DefineAccessor("writable",new CollectionMethod("get writable",0,(receiver,_) => Require(receiver).Pipe.Writable),1);
        }
        public override object Construct(object[] args) => new DecompressionStreamValue(StringValue(FunctionArgument(args,0)));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance,object[] args,JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);var result = (TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class DecompressionStreams {internal static readonly JsFunction Constructor = new DecompressionConstructor();}
}
