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
