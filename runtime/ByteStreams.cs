using System;

public static partial class Js
{
    static TypedArrayValue TransferByteChunk(object input)
    {
        BinaryBuffer buffer;int offset,length;
        if(input is TypedArrayValue array) {
            if(array.OutOfBounds)throw new InvalidOperationException("Byte stream chunk is detached or out of bounds");
            buffer=array.Buffer;offset=array.Offset;length=checked(array.Count*array.Size);
        } else if(input is BinaryView view) {
            if(view.OutOfBounds)throw new InvalidOperationException("Byte stream chunk is detached or out of bounds");
            buffer=view.Buffer;offset=view.Offset;length=view.Length;
        } else throw new InvalidOperationException("Byte stream chunks must be ArrayBuffer views");
        if(length==0)throw new InvalidOperationException("Byte stream chunks must not be empty");
        var transferred=(BinaryBuffer)TransferBuffer(buffer,Undefined,false);
        return new TypedArrayValue("Uint8Array",transferred,offset,length);
    }
    sealed class ByteStreamController(CustomReadableStream owner) : JsDescriptorObject
    {
        internal readonly CustomReadableStream Owner=owner;
        internal override TsObject PropertyPrototype => TryExplicitPrototype(out var value) ? value : ByteControllerMethods.Prototype;
    }
    static class ByteControllerMethods
    {
        internal static readonly TsObject Prototype=Create();
        internal static readonly JsFunction Constructor=CreateConstructor();
        static JsFunction CreateConstructor()
        {
            var value=new IllegalByteControllerConstructor();((JsDescriptorObject)Prototype).DefineMethod("constructor",value);return value;
        }
        static ByteStreamController Require(object value)=>value as ByteStreamController ?? throw new InvalidOperationException("Byte controller receiver required");
        static TsObject Create()
        {
            var value=new JsClassInstance {ParentPrototype=DefaultClassObjectPrototype};
            value.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableByteStreamController",false);
            value.DefineAccessor("desiredSize",new CollectionMethod("get desiredSize",0,(receiver,_)=>Require(receiver).Owner.DesiredSize),1);
            value.DefineAccessor("byobRequest",new CollectionMethod("get byobRequest",0,(receiver,_)=>{Require(receiver);return null;}),1);
            value.DefineOwn("enqueue",new CollectionMethod("enqueue",1,(receiver,args)=>{Require(receiver).Owner.Enqueue(FunctionArgument(args,0));return Undefined;}));
            value.DefineOwn("close",new CollectionMethod("close",0,(receiver,_)=>{Require(receiver).Owner.Close();return Undefined;}));
            value.DefineOwn("error",new CollectionMethod("error",0,(receiver,args)=>{Require(receiver).Owner.Error(FunctionArgument(args,0));return Undefined;}));
            return value;
        }
    }
    sealed class IllegalByteControllerConstructor : NativeNamespaceFunction
    {
        internal IllegalByteControllerConstructor() : base("ReadableByteStreamController",static _=>throw new InvalidOperationException("Illegal constructor"),0)
        {UseObjectPrototype(ByteControllerMethods.Prototype);}
        public override object Construct(object[] args)=>throw new InvalidOperationException("Illegal constructor");
    }
}
