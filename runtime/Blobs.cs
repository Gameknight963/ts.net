using System;
using System.IO;
using System.Text;

public static partial class Js
{
    class BlobValue(byte[] bytes, string type) : TsObject
    {
        internal readonly byte[] Bytes = bytes;
        internal readonly string Type = type;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : (TsObject)RuntimeNamespaces.Blob.Get("prototype");
    }
    static BlobValue RequireBlob(object value) => value as BlobValue ?? throw new InvalidOperationException("Blob receiver required");
    static string BlobType(object value)
    {
        if (IsUndefined(value)) return "";
        string type = StringValue(value);
        foreach (char ch in type) if (ch < 32 || ch > 126) return "";
        return type.ToLowerInvariant();
    }
    static BlobValue NewBlob(object[] args)
    {
        object options = FunctionArgument(args, 1);
        if (!IsNullish(options) && options is not TsObject) throw new InvalidOperationException("Blob options must be an object");
        object endingValue = options is TsObject settings ? settings.Get("endings") : Undefined;
        string endings = IsUndefined(endingValue) ? "transparent" : StringValue(endingValue);
        if (endings is not ("transparent" or "native")) throw new InvalidOperationException("Invalid Blob endings");
        string type = BlobType(options is TsObject bag ? bag.Get("type") : Undefined);
        object parts = FunctionArgument(args, 0);
        if (IsUndefined(parts)) return new BlobValue(Array.Empty<byte>(), type);
        if (parts is string || IsNullish(parts)) throw new InvalidOperationException("Blob parts must be a sequence");
        object source = Iteration.Start(parts); int position = 0;
        using var output = new MemoryStream();
        try
        {
            while (position < Iteration.Length(source))
            {
                object part = Iteration.Value(source, position); position += Iteration.Step(source, position);
                if (part is BlobValue blob) output.Write(blob.Bytes);
                else if (part is BinaryBuffer or BinaryView or TypedArrayValue) {
                    if (part is BinaryBuffer { Detached: true } or BinaryView { OutOfBounds: true } or TypedArrayValue { OutOfBounds: true }) throw new InvalidOperationException("Blob input buffer is detached or out of bounds");
                    output.Write(EncodingInput(part));
                }
                else
                {
                    string text = StringValue(part);
                    if (endings == "native") text = text.Replace("\r\n", "\n").Replace("\n", Environment.NewLine);
                    output.Write(Encoding.UTF8.GetBytes(text));
                }
            }
        }
        catch { CloseIterationAt(source, position, true); throw; }
        return new BlobValue(output.ToArray(), type);
    }
    sealed class BlobConstructor : NativeNamespaceFunction
    {
        internal BlobConstructor() : base("Blob", static _ => throw new InvalidOperationException("Blob requires new"), 0)
        {
            var prototype = new JsClassInstance { ParentPrototype = DefaultClassObjectPrototype }; UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor", this);
            prototype.DefineMethod(PropertyKey(ToStringTagSymbol), "Blob");
            prototype.DefineClassAccessor("size", new CollectionMethod("get size", 0, (receiver, _) => (double)RequireBlob(receiver).Bytes.Length), 1);
            prototype.DefineClassAccessor("type", new CollectionMethod("get type", 0, (receiver, _) => RequireBlob(receiver).Type), 1);
            prototype.DefineMethod("slice", new CollectionMethod("slice", 0, (receiver, args) =>
            {
                var blob = RequireBlob(receiver);
                int start = BufferSliceIndex(FunctionArgument(args, 0), blob.Bytes.Length);
                int end = IsUndefined(FunctionArgument(args, 1)) ? blob.Bytes.Length : BufferSliceIndex(args[1], blob.Bytes.Length);
                return new BlobValue(blob.Bytes.AsSpan(start, Math.Max(end - start, 0)).ToArray(), BlobType(FunctionArgument(args, 2)));
            }));
            prototype.DefineMethod("text", new CollectionMethod("text", 0, (receiver, _) =>
            {
                string text = Encoding.UTF8.GetString(RequireBlob(receiver).Bytes);
                if (text.Length != 0 && text[0] == '\ufeff') text = text[1..];
                var promise = new JsPromise(); promise.Settle(text, false); return promise;
            }));
            prototype.DefineMethod("arrayBuffer", new CollectionMethod("arrayBuffer", 0, (receiver, _) =>
            {
                var bytes = RequireBlob(receiver).Bytes;
                var buffer = new BinaryBuffer(bytes.Length); bytes.CopyTo(buffer.Bytes, 0);
                var promise = new JsPromise(); promise.Settle(buffer, false); return promise;
            }));
            prototype.DefineMethod("bytes", new CollectionMethod("bytes", 0, (receiver, _) =>
            {
                var bytes = RequireBlob(receiver).Bytes;
                var array = AllocateTypedArray("Uint8Array", bytes.Length); bytes.CopyTo(array.Buffer.Bytes, 0);
                var promise = new JsPromise(); promise.Settle(array, false); return promise;
            }));
        }
        public override object Construct(object[] args) => NewBlob(args);
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args) { var result = (TsObject)Construct(args); result.SetPrototype(((TsObject)instance).PropertyPrototype); return result; }
    }
}
