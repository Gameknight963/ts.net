using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

static object Call(TsObject receiver,string method,params object[] args) => ((JsFunction)receiver.Get(method)).InvokeWithReceiver(receiver,args);
static byte[] RuntimeStream(string builtin,string format,byte[] input,bool emptyWrite,int chunkSize=997,bool expectError=false)
{
    var bytes=new List<byte>();bool completed=false,rejected=false;object failure=null;
    int status=Js.Run(()=>{
        var compression=(TsObject)((JsFunction)Js.BuiltinNamespace(builtin)).Construct(new object[]{format});
        var sink=new JsDynamicObject();
        sink.Set("write",new JsFunction((FunctionBody)(args=>{
            var chunk=(TsObject)args[0];int length=(int)(double)chunk.Get("length");
            if(length==0)throw new Exception("Empty compressed chunk");
            for(int index=0;index<length;index++)bytes.Add((byte)(double)Js.IndexGet(chunk,(double)index));
            return Js.Undefined;
        }),null,"write",1));
        var destination=((JsFunction)Js.BuiltinNamespace("WritableStream")).Construct(new object[]{sink});
        var piping=(TsObject)Call((TsObject)compression.Get("readable"),"pipeTo",destination);
        Call(piping,"then",new JsFunction((FunctionBody)(_=>{completed=true;return Js.Undefined;}),null,"complete",1),new JsFunction((FunctionBody)(args=>{completed=true;rejected=true;failure=args[0];return Js.Undefined;}),null,"failed",1));
        var writer=(TsObject)Call((TsObject)compression.Get("writable"),"getWriter");
        var ignore=new JsFunction((FunctionBody)(_=>Js.Undefined),null,"handled",1);
        void Request(string method,params object[] args){var promise=(TsObject)Call(writer,method,args);if(expectError)Call(promise,"catch",ignore);}
        var constructor=(JsFunction)Js.BuiltinNamespace("Uint8Array");
        if(emptyWrite)Request("write",constructor.Construct(new object[]{0d}));
        for(int start=0;start<input.Length;start+=chunkSize){
            int count=Math.Min(chunkSize,input.Length-start);var chunk=constructor.Construct(new object[]{(double)count});
            for(int index=0;index<count;index++)Js.IndexSet(chunk,(double)index,(double)input[start+index]);
            Request("write",chunk);
        }
        Request("close");
    });
    if(status!=0 || !completed)throw new Exception(builtin+" pipeline failed");
    if(expectError){if(!rejected || !Equals(Js.Get(failure,"name"),"TypeError"))throw new Exception("Malformed compressed input was not rejected with TypeError");return Array.Empty<byte>();}
    if(rejected)throw new Exception(builtin+" rejected valid input: "+failure);
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
 const {format,data,original}=JSON.parse(input);
 const stream=new Blob([Buffer.from(data,'base64')]).stream().pipeThrough(new DecompressionStream(format));
 const bytes=await new Response(stream).arrayBuffer();
 const compressed=await new Response(new Blob([Buffer.from(original,'base64')]).stream().pipeThrough(new CompressionStream(format))).arrayBuffer();
 process.stdout.write(JSON.stringify({decoded:Buffer.from(bytes).toString('base64'),compressed:Buffer.from(compressed).toString('base64')}));
}catch(error){console.error(error);process.exitCode=1;}});
""";
try{
    var random=new byte[16384];new Random(42).NextBytes(random);
    byte[][] cases={Array.Empty<byte>(),Encoding.UTF8.GetBytes("hello \u20ac \ud83d\ude00"),Enumerable.Repeat((byte)65,100000).ToArray(),random};
    foreach(string format in new[]{"gzip","deflate","deflate-raw","brotli"})foreach(var (input,emptyWrite) in cases.Select(bytes=>(bytes,false)).Append((Array.Empty<byte>(),true))){
        byte[] compressed=RuntimeStream("CompressionStream",format,input,emptyWrite);
        if(!Decode(format,compressed).AsSpan().SequenceEqual(input))throw new Exception(format+" CLR roundtrip failed");
        var settings=new ProcessStartInfo("node"){RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
        settings.ArgumentList.Add("-e");settings.ArgumentList.Add(nodeScript);
        using var process=Process.Start(settings)??throw new Exception("Cannot start Node");
        process.StandardInput.Write(JsonSerializer.Serialize(new{format,data=Convert.ToBase64String(compressed),original=Convert.ToBase64String(input)}));process.StandardInput.Close();
        string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();process.WaitForExit();
        if(process.ExitCode!=0)throw new Exception(format+" Node roundtrip failed: "+error);
        using var reference=JsonDocument.Parse(output);
        if(!Convert.FromBase64String(reference.RootElement.GetProperty("decoded").GetString()!).AsSpan().SequenceEqual(input))throw new Exception(format+" Node decode mismatch");
        byte[] nodeCompressed=Convert.FromBase64String(reference.RootElement.GetProperty("compressed").GetString()!);
        if(!RuntimeStream("DecompressionStream",format,nodeCompressed,emptyWrite,1).AsSpan().SequenceEqual(input))throw new Exception(format+" Runtime decode mismatch");
    }
    using(var encoded=new MemoryStream()){
        using(var gzip=new GZipStream(encoded,CompressionLevel.Optimal,true))gzip.Write(new byte[]{1,2,3});
        byte[] member=encoded.ToArray();
        RuntimeStream("DecompressionStream","gzip",member.Concat(member).ToArray(),false,1,true);
        RuntimeStream("DecompressionStream","gzip",member[..^1],false,1,true);
    }
    Console.WriteLine("PASS gzip, zlib, raw deflate and Brotli interoperability in both directions, including empty, Unicode, random and byte-at-a-time input");
    Console.WriteLine("PASS incomplete gzip and concatenated gzip members are rejected");
}catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
