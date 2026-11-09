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
