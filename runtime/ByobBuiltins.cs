using System;

public static partial class Js
{
    static class ByobReaderMethods
    {
        internal static readonly TsObject Prototype=Create();
        internal static readonly JsFunction Constructor=CreateConstructor();
        static JsFunction CreateConstructor(){var result=new ByobReaderConstructor();((JsDescriptorObject)Prototype).DefineMethod("constructor",result);return result;}
        static ByobStreamReader Require(object value)=>value as ByobStreamReader ?? throw new InvalidOperationException("BYOB reader receiver required");
        static CustomReadableStream Owner(object value)=>Require(value).Owner ?? throw new InvalidOperationException("Reader lock has been released");
        static TsObject Create()
        {
            var result=new JsClassInstance{ParentPrototype=DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableStreamBYOBReader",false);
            result.DefineAccessor("closed",new CollectionMethod("get closed",0,(receiver,_)=>BodyPromiseOperation(()=>Require(receiver).ClosedPromise)),1);
            result.DefineOwn("read",new CollectionMethod("read",1,(receiver,args)=>BodyPromiseOperation(()=>Owner(receiver).ReadInto(FunctionArgument(args,0),FunctionArgument(args,1),Require(receiver)))));
            result.DefineOwn("cancel",new CollectionMethod("cancel",1,(receiver,args)=>BodyPromiseOperation(()=>Owner(receiver).Cancel(FunctionArgument(args,0)))));
            result.DefineOwn("releaseLock",new CollectionMethod("releaseLock",0,(receiver,_)=>{Require(receiver).Release();return Undefined;}));
            return result;
        }
    }
    sealed class ByobReaderConstructor : NativeNamespaceFunction
    {
        internal ByobReaderConstructor() : base("ReadableStreamBYOBReader",static _=>throw new InvalidOperationException("BYOB reader requires new"),1){UseObjectPrototype(ByobReaderMethods.Prototype);}
        public override object Construct(object[] args)=>FunctionArgument(args,0) is CustomReadableStream stream ? stream.GetByobReader() : throw new InvalidOperationException("Byte stream required");
        internal override bool CanInitializeDerived=>true;
        internal override object InitializeDerived(object instance,object[] args,JsFunction newTarget){var value=(TsObject)Construct(args);value.SetPrototype(ConstructionPrototype(newTarget));return value;}
    }
    static class ByobRequestMethods
    {
        internal static readonly TsObject Prototype=Create();
        internal static readonly JsFunction Constructor=CreateConstructor();
        static JsFunction CreateConstructor(){var value=new IllegalByobRequestConstructor();((JsDescriptorObject)Prototype).DefineMethod("constructor",value);return value;}
        static ByteReadRequest Require(object value)=>value as ByteReadRequest ?? throw new InvalidOperationException("BYOB request receiver required");
        static CustomReadableStream Owner(object value)=>Require(value).Owner ?? throw new InvalidOperationException("BYOB request is no longer active");
        static TsObject Create()
        {
            var result=new JsClassInstance{ParentPrototype=DefaultClassObjectPrototype};
            result.DefineMethod(PropertyKey(ToStringTagSymbol),"ReadableStreamBYOBRequest",false);
            result.DefineAccessor("view",new CollectionMethod("get view",0,(receiver,_)=>Require(receiver).View),1);
            result.DefineOwn("respond",new CollectionMethod("respond",1,(receiver,args)=>{Require(receiver);double count=ByobByteCount(FunctionArgument(args,0));Owner(receiver).RespondBytes(count);return Undefined;}));
            result.DefineOwn("respondWithNewView",new CollectionMethod("respondWithNewView",1,(receiver,args)=>{Owner(receiver).RespondView(FunctionArgument(args,0));return Undefined;}));
            return result;
        }
    }
    sealed class IllegalByobRequestConstructor : NativeNamespaceFunction
    {
        internal IllegalByobRequestConstructor() : base("ReadableStreamBYOBRequest",static _=>throw new InvalidOperationException("Illegal constructor"),0){UseObjectPrototype(ByobRequestMethods.Prototype);}
        public override object Construct(object[] args)=>throw new InvalidOperationException("Illegal constructor");
    }
}
