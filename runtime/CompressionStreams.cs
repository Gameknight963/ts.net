using System;
using System.IO;
using System.IO.Compression;

public static partial class Js
{
    sealed class CompressionState : IDisposable
    {
        readonly MemoryStream output = new();
        readonly string format;
        bool hadInput, emitted;
        Stream compressor;
        internal CompressionState(string format)
        {
            this.format = format;
            compressor = format switch {
                "gzip" => new GZipStream(output,CompressionLevel.Optimal,true),
                "deflate" => new ZLibStream(output,CompressionLevel.Optimal,true),
                "deflate-raw" => new DeflateStream(output,CompressionLevel.Optimal,true),
                "brotli" => new BrotliStream(output,CompressionLevel.Optimal,true),
                _ => throw new InvalidOperationException("Unsupported compression format")
            };
        }
        internal void Write(object input, TransformController controller)
        {
            try {
                if (input is not (BinaryBuffer or BinaryView or TypedArrayValue)) throw new InvalidOperationException("Compression chunks must be buffers or views");
                var bytes = EncodingInput(input);hadInput |= !bytes.IsEmpty;
                compressor.Write(bytes);Emit(controller);
            }
            catch {Dispose();throw;}
        }
        internal void Finish(TransformController controller)
        {
            try {
                compressor.Dispose();compressor = null;
                // Some CLR versions emit nothing for an empty deflate stream.
                // These are complete empty streams, including wrapper checksums.
                if (!hadInput && !emitted && output.Length == 0) output.Write(format switch {
                    "gzip" => new byte[] {31,139,8,0,0,0,0,0,0,255,3,0,0,0,0,0,0,0,0,0},
                    "deflate" => new byte[] {120,156,3,0,0,0,0,1},
                    "deflate-raw" => new byte[] {3,0},
                    _ => new byte[] {59}
                });
                Emit(controller);
            }
            finally {Dispose();}
        }
        void Emit(TransformController controller)
        {
            int count = checked((int)output.Length);
            if (count == 0) return;
            var chunk = AllocateTypedArray("Uint8Array",count);output.GetBuffer().AsSpan(0,count).CopyTo(chunk.Buffer.Bytes);
            emitted = true;
            output.SetLength(0);output.Position = 0;controller.Owner.Enqueue(chunk);
        }
        public void Dispose()
        {
            var current = compressor;compressor = null;
            try {current?.Dispose();}finally {output.Dispose();}
        }
    }
    sealed class CompressionStreamValue : JsDescriptorObject
    {
        internal readonly TransformStreamValue Pipe;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var prototype) ? prototype : (TsObject)CompressionStreams.Constructor.Get("prototype");
        internal CompressionStreamValue(string format)
        {
            var state = new CompressionState(format);var transformer = new JsClassInstance();
            transformer.DefineOwn("transform",new JsFunction((FunctionBody)(args => {state.Write(FunctionArgument(args,0),(TransformController)FunctionArgument(args,1));return Undefined;}),null,"transform",2));
            transformer.DefineOwn("flush",new JsFunction((FunctionBody)(args => {state.Finish((TransformController)FunctionArgument(args,0));return Undefined;}),null,"flush",1));
            transformer.DefineOwn("cancel",new JsFunction((FunctionBody)(_ => {state.Dispose();return Undefined;}),null,"cancel",1));
            try {Pipe = new TransformStreamValue(transformer,Undefined,Undefined);}catch {state.Dispose();throw;}
        }
    }
    sealed class CompressionConstructor : NativeNamespaceFunction
    {
        internal CompressionConstructor() : base("CompressionStream",static _ => throw new InvalidOperationException("CompressionStream requires new"),1)
        {
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this);prototype.DefineMethod(PropertyKey(ToStringTagSymbol),"CompressionStream",false);
            CompressionStreamValue Require(object receiver) => receiver as CompressionStreamValue ?? throw new InvalidOperationException("Compression stream receiver required");
            prototype.DefineAccessor("readable",new CollectionMethod("get readable",0,(receiver,_) => Require(receiver).Pipe.Readable),1);
            prototype.DefineAccessor("writable",new CollectionMethod("get writable",0,(receiver,_) => Require(receiver).Pipe.Writable),1);
        }
        public override object Construct(object[] args) => new CompressionStreamValue(StringValue(FunctionArgument(args,0)));
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);var result = (TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class CompressionStreams {internal static readonly JsFunction Constructor = new CompressionConstructor();}
}
