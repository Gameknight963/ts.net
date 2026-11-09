using System.Numerics;
using System.Reflection;
var numbers=new NumberArray(5);
numbers.SetNumberAt(0,double.NaN);numbers.SetNumberAt(1,null);numbers.SetNumberAt(2,-0.0);numbers.SetNumberAt(3,double.PositiveInfinity);
foreach (var pair in new[]{(0,false),(1,true),(2,false),(3,false),(4,true),(5,true)})
    if(Js.NumberAtIsNullish(numbers,pair.Item1)!=pair.Item2) throw new Exception("Numeric marker mismatch");
var generic=new JsArray(new object[]{Js.Undefined,1.0,double.NaN});
if(!Js.NumberAtIsNullish(generic,0)||Js.NumberAtIsNullish(generic,1)||Js.NumberAtIsNullish(generic,2)||!Js.NumberAtIsNullish(generic,8)) throw new Exception("Generic array fallback mismatch");
foreach(object value in new object[]{null,"wrong",true}) {
    bool failed=false;
    try {Js.NumberAtIsNullish(new JsArray(new[]{value}),0);} catch(InvalidCastException) {failed=true;}
    if(!failed) throw new Exception("Numeric read skipped its checked conversion");
}
var assembly=Assembly.LoadFile(Path.GetFullPath(args[0]));
var method=assembly.GetType("Program")!.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Single(m=>m.ReturnType==typeof(double?)&&m.GetParameters().Length==2&&m.GetParameters().All(p=>p.ParameterType==typeof(double?)));
var kernel=method.CreateDelegate<Func<double?,double?,double?>>();
for(int i=0;i<200;i++) kernel(10000,0);
long start=GC.GetAllocatedBytesForCurrentThread();var result=kernel(1000000,0);long bytes=GC.GetAllocatedBytesForCurrentThread()-start;
if(result!=250000||bytes>=1024) throw new Exception($"Nullish loop result/allocation mismatch: {result}, {bytes} bytes");
Console.WriteLine($"PASS numeric markers, checked array fallbacks, and nullable loop allocations ({bytes} bytes)");

static void Check(object left, object right, bool expected, string label) {
    if (Js.Equal(left,right)!=expected) throw new Exception("Equality mismatch: "+label);
}
object nan=double.NaN;
Check(nan,nan,false,"same boxed NaN");
Check(nan,double.NaN,false,"different boxed NaN");
Check(0d,-0d,true,"signed zeros");
Check(1d,1d,true,"different boxed numbers");
Check(1d,2d,false,"different numbers");
Check(1d,"1",false,"number and string");
Check(1d,BigInteger.One,false,"number and bigint");
Check(BigInteger.One,BigInteger.One,true,"different boxed bigint");
Check(new string('x',20),new string('x',20),true,"different string references");
Check(null,Js.Undefined,false,"null and undefined");
var first=new JsDynamicObject();var second=new JsDynamicObject();
Check(first,second,false,"different objects");
object[] aliases={null,Js.Undefined,false,true,0d,-0d,42d,double.PositiveInfinity,double.NegativeInfinity,BigInteger.One,"text",first,new JsFunction((FunctionBody)(static _=>null),null,"",0)};
foreach(object value in aliases)Check(value,value,true,"aliased value");
Console.WriteLine("PASS strict equality aliases, NaN, signed zero, primitive types, and object identity");

double[] boxingValues = {-129d,-128,-127,-1,-0d,0,1,127,128,254,255,256,-128.5,-127.5,-0.5,0.5,255.5,double.Epsilon,double.MinValue,double.MaxValue,double.PositiveInfinity,double.NegativeInfinity,double.NaN};
void CheckBox(double number) {
    if(Js.BoxNumber(number) is not double boxed || BitConverter.DoubleToInt64Bits(boxed)!=BitConverter.DoubleToInt64Bits(number))
        throw new Exception("Number boxing changed its value or bits");
}
foreach(double number in boxingValues)CheckBox(number);
ulong randomBits=0x963963963963963;
for(int i=0;i<10000;i++) {
    randomBits^=randomBits<<13;randomBits^=randomBits>>7;randomBits^=randomBits<<17;
    CheckBox(BitConverter.Int64BitsToDouble(unchecked((long)randomBits)));
}
if(!ReferenceEquals(Js.BoxNumber(null),Js.Undefined))throw new Exception("Missing number boxing changed");
Console.WriteLine("PASS exact number boxing, signed zero, random bit patterns, and missing numbers");
