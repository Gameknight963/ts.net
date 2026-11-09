using System.Reflection;

static object Call(TsObject value,string name,params object[] args)=>((JsFunction)value.Get(name)).InvokeWithReceiver(value,args);
bool complete=false;
int status=Js.Run(()=>{
    var input=(TsObject)((JsFunction)Js.BuiltinNamespace("Uint8Array")).Construct(new object[]{4d});
    var sourceBuffer=(TsObject)input.Get("buffer");
    var storage=sourceBuffer.GetType().GetField("Bytes",BindingFlags.Instance|BindingFlags.NonPublic)!;
    var original=(byte[])storage.GetValue(sourceBuffer)!;
    original[0]=17;original[3]=42;
    var source=new JsDynamicObject();source.Set("type","bytes");
    source.Set("start",new JsFunction((FunctionBody)(args=>{
        var controller=(TsObject)args[0];Call(controller,"enqueue",input);Call(controller,"close");return Js.Undefined;
    }),null,"start",1));
    var stream=(TsObject)((JsFunction)Js.BuiltinNamespace("ReadableStream")).Construct(new object[]{source});
    if((double)sourceBuffer.Get("byteLength")!=0)throw new Exception("Source buffer was not detached");
    var reader=(TsObject)Call(stream,"getReader");
    var read=(TsObject)Call(reader,"read");
    Call(read,"then",new JsFunction((FunctionBody)(args=>{
        var result=(TsObject)args[0];var chunk=(TsObject)result.Get("value");var buffer=(TsObject)chunk.Get("buffer");
        if(!ReferenceEquals(original,storage.GetValue(buffer)))throw new Exception("Enqueue copied the backing byte array");
        if((double)Js.IndexGet(chunk,0d)!=17 || (double)Js.IndexGet(chunk,3d)!=42)throw new Exception("Transferred bytes changed");
        complete=true;return Js.Undefined;
    }),null,"read",1));
});
if(status!=0 || !complete)throw new Exception("Byte stream transfer check did not complete");
Console.WriteLine("PASS byte stream enqueue transfers ownership without copying the backing byte array");
complete=false;
status=Js.Run(()=>{
    var input=(TsObject)((JsFunction)Js.BuiltinNamespace("Uint8Array")).Construct(new object[]{8d});
    var sourceBuffer=(TsObject)input.Get("buffer");
    var storage=sourceBuffer.GetType().GetField("Bytes",BindingFlags.Instance|BindingFlags.NonPublic)!;
    var original=(byte[])storage.GetValue(sourceBuffer)!;
    var source=new JsDynamicObject();source.Set("type","bytes");
    source.Set("pull",new JsFunction((FunctionBody)(args=>{
        var controller=(TsObject)args[0];var request=(TsObject)controller.Get("byobRequest");var view=(TsObject)request.Get("view");
        foreach(double invalid in new[]{-1d,double.PositiveInfinity}) {
            bool rejected=false;try{Call(request,"respond",invalid);}catch(InvalidOperationException){rejected=true;}
            if(!rejected || request.Get("view")==null)throw new Exception("Invalid BYOB count changed the pending request");
        }
        if(!ReferenceEquals(original,storage.GetValue(view.Get("buffer"))))throw new Exception("BYOB request copied the caller buffer");
        Js.IndexSet(view,0d,42d);Call(request,"respond",1d);Call(controller,"close");return Js.Undefined;
    }),null,"pull",1));
    var stream=(TsObject)((JsFunction)Js.BuiltinNamespace("ReadableStream")).Construct(new object[]{source});
    var options=new JsDynamicObject();options.Set("mode","byob");var reader=(TsObject)Call(stream,"getReader",options);
    var read=(TsObject)Call(reader,"read",input);
    if((double)sourceBuffer.Get("byteLength")!=0)throw new Exception("BYOB read did not detach the caller buffer");
    Call(read,"then",new JsFunction((FunctionBody)(args=>{
        var view=(TsObject)((TsObject)args[0]).Get("value");
        if(!ReferenceEquals(original,storage.GetValue(view.Get("buffer"))))throw new Exception("BYOB response copied the caller buffer");
        if((double)Js.IndexGet(view,0d)!=42)throw new Exception("BYOB bytes changed");complete=true;return Js.Undefined;
    }),null,"read",1));
});
if(status!=0 || !complete)throw new Exception("BYOB transfer check did not complete");
Console.WriteLine("PASS BYOB requests and responses preserve the caller's backing byte array");
bool readFailed=false,closedFailed=false,defaultFailed=false,cloneFailed=false,requestFailed=false;object storedFailure=null;
status=Js.Run(()=>{
    var payload=(TsObject)((JsFunction)Js.BuiltinNamespace("Uint8Array")).Construct(new object[]{1d});
    var response=(TsObject)((JsFunction)Js.BuiltinNamespace("Response")).Construct(new object[]{payload});
    var body=(TsObject)response.Get("body");var options=new JsDynamicObject();options.Set("mode","byob");
    var reader=(TsObject)Call(body,"getReader",options);
    JsFunction Failure(Action mark)=>new((FunctionBody)(args=>{
        if(!Equals(Js.Get(args[0],"name"),"TypeError"))throw new Exception("Expected TypeError");
        if(storedFailure==null)storedFailure=args[0];else if(!ReferenceEquals(storedFailure,args[0]))throw new Exception("Stored body error identity changed");
        mark();return Js.Undefined;
    }),null,"failed",1);
    var view=((JsFunction)Js.BuiltinNamespace("Uint16Array")).Construct(new object[]{2d});
    Call((TsObject)Call(reader,"read",view),"catch",Failure(()=>readFailed=true));
    Call((TsObject)reader.Get("closed"),"catch",Failure(()=>closedFailed=true));
    Call(reader,"releaseLock");var ordinary=(TsObject)Call(body,"getReader");
    Call((TsObject)Call(ordinary,"read"),"catch",Failure(()=>defaultFailed=true));
    Call(ordinary,"releaseLock");var branches=(TsObject)Call(body,"tee");
    var errored=(TsObject)((JsFunction)Js.BuiltinNamespace("Response")).Construct(new[]{branches.Get("0")});
    var cloned=(TsObject)Call(errored,"clone");Call((TsObject)Call(cloned,"text"),"catch",Failure(()=>cloneFailed=true));
    var init=new JsDynamicObject();init.Set("method","POST");init.Set("body",branches.Get("1"));init.Set("duplex","half");
    var request=(TsObject)((JsFunction)Js.BuiltinNamespace("Request")).Construct(new object[]{"https://example.com/",init});
    var requestClone=Call(request,"clone");var transferred=(TsObject)((JsFunction)Js.BuiltinNamespace("Request")).Construct(new[]{requestClone});
    Call((TsObject)Call(transferred,"text"),"catch",Failure(()=>requestFailed=true));
});
if(status!=0 || !readFailed || !closedFailed || !defaultFailed || !cloneFailed || !requestFailed)throw new Exception("Buffered BYOB alignment error did not propagate");
Console.WriteLine("PASS buffered BYOB alignment errors reject read and closed promises and survive reader replacement");
Console.WriteLine("PASS stored body errors survive tee, Response cloning, and Request cloning and transfer");
