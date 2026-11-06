using System.Reflection;
using System.Reflection.Emit;
var assembly=Assembly.LoadFile(Path.GetFullPath(args[0]));
var methods=assembly.GetType("Program")!.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
var opcodes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
    .Select(f => (OpCode)f.GetValue(null)!).ToDictionary(op => (ushort)op.Value);
bool Has(MethodInfo method, OpCode expected, string name=null)
{
    var bytes = method.GetMethodBody()!.GetILAsByteArray()!;
    for (int i = 0; i < bytes.Length;)
    {
        ushort code = bytes[i++];
        if (code == 0xfe) code = (ushort)(0xfe00 | bytes[i++]);
        var op = opcodes[code];
        if (op == expected && (name == null || method.Module.ResolveMethod(BitConverter.ToInt32(bytes, i)) is MethodInfo called && called.DeclaringType == typeof(Js) && called.Name == name)) return true;
        i += op.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => throw new Exception("Unexpected switch table"),
            _ => 4
        };
    }
    return false;
}

var fast=methods.Single(m=>m.ReturnType==typeof(double?) && m.GetParameters().Length==1 && m.GetParameters()[0].ParameterType==typeof(double?));
var general=methods.Single(m=>m.ReturnType==typeof(double?) && m.GetParameters().Length==2);
if(!Has(fast,OpCodes.Rem_Un)||Has(fast,OpCodes.Rem)||!Has(general,OpCodes.Call,"Remainder")) throw new Exception("Remainder IL selection mismatch");
var kernel=fast.CreateDelegate<Func<double?,double?>>();
var random=new Random(963);
for(int i=0;i<20000;i++) {
    double value=BitConverter.Int64BitsToDouble(random.NextInt64(long.MinValue,long.MaxValue));
    double expected=(Js.ToInt32(value)&255)%26;
    if(kernel(value)!=expected) throw new Exception("Masked remainder mismatch");
}
for(int i=0;i<10000;i++)kernel(i);
long start=GC.GetAllocatedBytesForCurrentThread();
for(int i=0;i<1000000;i++)kernel(i);
long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
if(bytes!=0)throw new Exception($"Remainder allocated {bytes} bytes");
Console.WriteLine("PASS integer remainder IL, general numeric fallback, randomized inputs, and allocation-free calls");
