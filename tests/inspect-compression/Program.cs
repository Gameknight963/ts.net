using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

static object Call(TsObject receiver,string method,params object[] args) => ((JsFunction)receiver.Get(method)).InvokeWithReceiver(receiver,args);
static byte[] Compress(string format,byte[] input,bool emptyWrite)
{
    var bytes=new List<byte>();bool completed=false;
    int status=Js.Run(()=>{
        var compression=(TsObject)((JsFunction)Js.BuiltinNamespace("CompressionStream")).Construct(new object[]{format});
        var sink=new JsDynamicObject();
        sink.Set("write",new JsFunction((FunctionBody)(args=>{
            var chunk=(TsObject)args[0];int length=(int)(double)chunk.Get("length");
            if(length==0)throw new Exception("Empty compressed chunk");
            for(int index=0;index<length;index++)bytes.Add((byte)(double)Js.IndexGet(chunk,(double)index));
            return Js.Undefined;
        }),null,"write",1));
        var destination=((JsFunction)Js.BuiltinNamespace("WritableStream")).Construct(new object[]{sink});
        var piping=(TsObject)Call((TsObject)compression.Get("readable"),"pipeTo",destination);
        Call(piping,"then",new JsFunction((FunctionBody)(_=>{completed=true;return Js.Undefined;}),null,"complete",1));
        var writer=(TsObject)Call((TsObject)compression.Get("writable"),"getWriter");
        var constructor=(JsFunction)Js.BuiltinNamespace("Uint8Array");
        if(emptyWrite)Call(writer,"write",constructor.Construct(new object[]{0d}));
        for(int start=0;start<input.Length;start+=997){
            int count=Math.Min(997,input.Length-start);var chunk=constructor.Construct(new object[]{(double)count});
            for(int index=0;index<count;index++)Js.IndexSet(chunk,(double)index,(double)input[start+index]);
            Call(writer,"write",chunk);
        }
        Call(writer,"close");
    });
    if(status!=0 || !completed)throw new Exception("Compression pipeline failed");
    return bytes.ToArray();
}
static byte[] Decode(string format,byte[] bytes)
{
    using var input=new MemoryStream(bytes);
    using Stream decoder=format switch{
        "gzip"=>new GZipStream(input,CompressionMode.Decompress),
        "deflate"=>new ZLibStream(input,CompressionMode.Decompress),
        "deflate-raw"=>new DeflateStream(input,CompressionMode.Decompress),
        _=>new BrotliStream(input,CompressionMode.Decompress)
    };
    using var output=new MemoryStream();decoder.CopyTo(output);return output.ToArray();
}
const string nodeScript="""
let input='';process.stdin.setEncoding('utf8');process.stdin.on('data',chunk=>input+=chunk);
process.stdin.on('end',async()=>{try{
 const {format,data}=JSON.parse(input);
 const stream=new Blob([Buffer.from(data,'base64')]).stream().pipeThrough(new DecompressionStream(format));
 const bytes=await new Response(stream).arrayBuffer();process.stdout.write(Buffer.from(bytes).toString('base64'));
}catch(error){console.error(error);process.exitCode=1;}});
""";
try{
    var random=new byte[16384];new Random(42).NextBytes(random);
    byte[][] cases={Array.Empty<byte>(),Encoding.UTF8.GetBytes("hello \u20ac \ud83d\ude00"),Enumerable.Repeat((byte)65,100000).ToArray(),random};
    foreach(string format in new[]{"gzip","deflate","deflate-raw","brotli"})foreach(var (input,emptyWrite) in cases.Select(bytes=>(bytes,false)).Append((Array.Empty<byte>(),true))){
        byte[] compressed=Compress(format,input,emptyWrite);
        if(!Decode(format,compressed).AsSpan().SequenceEqual(input))throw new Exception(format+" CLR roundtrip failed");
        var settings=new ProcessStartInfo("node"){RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
        settings.ArgumentList.Add("-e");settings.ArgumentList.Add(nodeScript);
        using var process=Process.Start(settings)??throw new Exception("Cannot start Node");
        process.StandardInput.Write(JsonSerializer.Serialize(new{format,data=Convert.ToBase64String(compressed)}));process.StandardInput.Close();
        string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();process.WaitForExit();
        if(process.ExitCode!=0 || !Convert.FromBase64String(output).AsSpan().SequenceEqual(input))throw new Exception(format+" Node roundtrip failed for "+input.Length+" bytes: "+error);
    }
    Console.WriteLine("PASS gzip, zlib, raw deflate and Brotli output decoded by CLR and Node, including empty, Unicode, random and multi-chunk input");
}catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
