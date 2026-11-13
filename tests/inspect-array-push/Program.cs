using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

try
{
    string path=Path.GetFullPath(args[0]);
    var assembly=AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
    var kernel=assembly.GetType("Program")!.GetMethods(BindingFlags.Public|BindingFlags.Static)
        .Single(method=>method.Name.StartsWith("kernel_f",StringComparison.Ordinal));
    var opcodes=typeof(OpCodes).GetFields().Where(field=>field.FieldType==typeof(OpCode))
        .Select(field=>(OpCode)field.GetValue(null)!).ToDictionary(op=>(ushort)op.Value);
    var calls=new HashSet<string>();var bytes=kernel.GetMethodBody()!.GetILAsByteArray()!;
    for(int position=0;position<bytes.Length;)
    {
        ushort code=bytes[position++];if(code==0xfe)code=(ushort)(0xfe00|bytes[position++]);
        var opcode=opcodes[code];
        if(opcode==OpCodes.Call && kernel.Module.ResolveMethod(BitConverter.ToInt32(bytes,position)) is MethodInfo target)
            calls.Add(target.Name);
        position+=opcode.OperandType switch {
            OperandType.InlineNone=>0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar=>1,
            OperandType.InlineVar=>2,
            OperandType.InlineI8 or OperandType.InlineR=>8,
            OperandType.InlineSwitch=>4+4*BitConverter.ToInt32(bytes,position),
            _=>4
        };
    }
    if(!calls.Contains("GuardPrivateArrayPush") || !calls.Contains("ArrayPushGuarded") || !calls.Contains("IsArrayBuiltin"))
        throw new Exception("Private array loop lacks its guarded append or ordinary fallback");
    var output=Console.Out;
    try {
        Console.SetOut(TextWriter.Null);var entry=assembly.EntryPoint!;
        object? result=entry.Invoke(null,entry.GetParameters().Length==0?null:[Array.Empty<string>()]);
        if(result is int code && code!=0)throw new Exception("Fixture initialization failed");
    } finally {Console.SetOut(output);}
    var run=kernel.CreateDelegate<Func<double?,double?,double?>>();
    for(int i=0;i<100;i++)run(1000,7);
    long before=GC.GetAllocatedBytesForCurrentThread();double? checksum=run(10000,7);
    long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
    if(checksum!=100060000 || allocated>564000)throw new Exception($"Wrong checksum {checksum} or excessive allocation {allocated}");
    var fresh=new JsArray(0);
    if(!Js.GuardPrivateArrayPush(fresh,1000) || fresh.Length()!=0)throw new Exception("Guard changed array length or rejected an ordinary array");
    foreach(double limit in new[]{0,-1,double.NaN,double.PositiveInfinity,1048577})
        if(Js.GuardPrivateArrayPush(new JsArray(0),limit))throw new Exception("Guard accepted an invalid size");
    var overridden=new JsArray(0);overridden.SetOwnValue("push",Js.Undefined);
    if(Js.GuardPrivateArrayPush(overridden,1))throw new Exception("Guard accepted an own push override");
    var derived=new DerivedArray();
    if(Js.GuardPrivateArrayPush(derived,1) || derived.Reads!=0)throw new Exception("Guard inspected or accepted a CLR subclass");
    Console.WriteLine("PASS guarded private array append, ordinary fallback, checksums and bounded allocation");
    return 0;
}
catch(Exception error){Console.Error.WriteLine(error.Message);return 1;}

sealed class DerivedArray():JsArray(0)
{
    public int Reads;
    public override object Get(string key){Reads++;return base.Get(key);}
}
