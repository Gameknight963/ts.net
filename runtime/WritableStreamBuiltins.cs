using System;

public static partial class Js
{
    static WritableStreamValue RequireWritable(object receiver) => receiver as WritableStreamValue ?? throw new InvalidOperationException("Writable stream receiver required");
    static WritableWriter RequireWriter(object receiver) => receiver as WritableWriter ?? throw new InvalidOperationException("Writable writer receiver required");
    static WritableController RequireWritableController(object receiver) => receiver as WritableController ?? throw new InvalidOperationException("Writable controller receiver required");
    sealed class WritableConstructor : NativeNamespaceFunction
    {
        readonly int kind;
        internal WritableConstructor(int kind) : base(kind == 0 ? "WritableStream" : kind == 1 ? "WritableStreamDefaultWriter" : "WritableStreamDefaultController",static _ => throw new InvalidOperationException("Constructor requires new"),kind == 1 ? 1 : 0)
        {
            this.kind = kind;
            var prototype = new JsClassInstance {ParentPrototype = DefaultClassObjectPrototype};UseObjectPrototype(prototype);
            prototype.DefineMethod("constructor",this);prototype.DefineMethod(PropertyKey(ToStringTagSymbol),kind == 0 ? "WritableStream" : kind == 1 ? "WritableStreamDefaultWriter" : "WritableStreamDefaultController",false);
            if (kind == 0) {
                prototype.DefineAccessor("locked",new CollectionMethod("get locked",0,(receiver,_) => BoxBoolean(RequireWritable(receiver).Writer != null)),1);
                prototype.DefineOwn("getWriter",new CollectionMethod("getWriter",0,(receiver,_) => RequireWritable(receiver).GetWriter()));
                prototype.DefineOwn("close",new CollectionMethod("close",0,(receiver,_) => BodyPromiseOperation(() => {
                    var stream = RequireWritable(receiver);if (stream.Writer != null) throw new InvalidOperationException("Writable stream is locked");return stream.Close();
                })));
                prototype.DefineOwn("abort",new CollectionMethod("abort",1,(receiver,args) => BodyPromiseOperation(() => {
                    var stream = RequireWritable(receiver);if (stream.Writer != null) throw new InvalidOperationException("Writable stream is locked");return stream.Abort(FunctionArgument(args,0));
                })));
            }
            else if (kind == 1) {
                prototype.DefineAccessor("closed",new CollectionMethod("get closed",0,(receiver,_) => BodyPromiseOperation(() => RequireWriter(receiver).Closed)),1);
                prototype.DefineAccessor("ready",new CollectionMethod("get ready",0,(receiver,_) => BodyPromiseOperation(() => RequireWriter(receiver).Ready)),1);
                prototype.DefineAccessor("desiredSize",new CollectionMethod("get desiredSize",0,(receiver,_) => RequireWriter(receiver).RequireOwner().DesiredSize),1);
                prototype.DefineOwn("write",new CollectionMethod("write",1,(receiver,args) => BodyPromiseOperation(() => {
                    var writer = RequireWriter(receiver);return writer.RequireOwner().Write(writer,FunctionArgument(args,0));
                })));
                prototype.DefineOwn("close",new CollectionMethod("close",0,(receiver,_) => BodyPromiseOperation(() => RequireWriter(receiver).RequireOwner().Close())));
                prototype.DefineOwn("abort",new CollectionMethod("abort",1,(receiver,args) => BodyPromiseOperation(() => RequireWriter(receiver).RequireOwner().Abort(FunctionArgument(args,0)))));
                prototype.DefineOwn("releaseLock",new CollectionMethod("releaseLock",0,(receiver,_) => {RequireWriter(receiver).Release();return Undefined;}));
            }
            else {
                prototype.DefineAccessor("signal",new CollectionMethod("get signal",0,(receiver,_) => RequireWritableController(receiver).Signal),1);
                prototype.DefineOwn("error",new CollectionMethod("error",0,(receiver,args) => {RequireWritableController(receiver).Owner.Error(FunctionArgument(args,0));return Undefined;}));
            }
        }
        public override object Construct(object[] args) => kind switch {
            0 => new WritableStreamValue(FunctionArgument(args,0),FunctionArgument(args,1)),
            1 => RequireWritable(FunctionArgument(args,0)).GetWriter(),
            _ => throw new InvalidOperationException("Illegal constructor")
        };
        internal override bool CanInitializeDerived => true;
        internal override object InitializeDerived(object instance, object[] args, JsFunction newTarget)
        {
            var prototype = ConstructionPrototype(newTarget);var result = (TsObject)Construct(args);result.SetPrototype(prototype);return result;
        }
    }
    static class WritableBuiltins
    {
        internal static readonly JsFunction Stream = new WritableConstructor(0), Writer = new WritableConstructor(1), Controller = new WritableConstructor(2);
    }
}
