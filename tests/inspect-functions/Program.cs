using System.Reflection;

_ = typeof(Js).Assembly;
var functions = Assembly.LoadFile(Path.GetFullPath(args[0]));
var types = functions.GetTypes().Where(t => t.BaseType == typeof(JsFunction)).ToArray();
foreach (int arity in new[] { 0, 1, 2 }) {
    if (!types.Any(t => t.IsSealed && t.GetMethod("InvokeNumber" + arity)!.DeclaringType == t))
        throw new Exception($"Missing generated numeric callback class for arity {arity}");
}
var assembly = Assembly.LoadFile(Path.GetFullPath(args[1]));
var method = assembly.GetType("Program")!.GetMethods().Single(m =>
    m.ReturnType == typeof(double?) && m.GetParameters().Length == 2 &&
    m.GetParameters().All(p => p.ParameterType == typeof(double?)));
var kernel = method.CreateDelegate<Func<double?, double?, double?>>();
for (int i = 0; i < 30; i++) kernel(1000, i);
long before = GC.GetAllocatedBytesForCurrentThread();
var small = kernel(1, 3);
long smallBytes = GC.GetAllocatedBytesForCurrentThread() - before;
before = GC.GetAllocatedBytesForCurrentThread();
var large = kernel(100000, 3);
long largeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
if (small != 4 || large != 100003 || largeBytes != smallBytes)
    throw new Exception($"Numeric callbacks changed results or allocated per call: {small}, {large}, {smallBytes}, {largeBytes}");
Console.WriteLine("PASS generated numeric callback overrides and no allocation per repeated call");

var boundTarget=new JsFunction((FunctionBody)(static values=>(double)values[0]+(double)values[1]),(RawNumberFunction2)(static (first,second)=>first+second),"add",2);
var bound=(JsFunction)Js.CallMethod(boundTarget.Get("bind"),boundTarget,new object[]{Js.Undefined,3d});
for(int i=0;i<30;i++)RunBoundCalls(bound,1000);
before=GC.GetAllocatedBytesForCurrentThread();
double boundSum=RunBoundCalls(bound,100000);
long boundBytes=GC.GetAllocatedBytesForCurrentThread()-before;
if(boundSum!=5000250000d || boundBytes!=0)throw new Exception($"Bound numeric calls changed results or allocated: {boundSum}, {boundBytes}");
Console.WriteLine("PASS bound numeric callbacks: matching results and zero allocations for 100000 calls");
static double RunBoundCalls(JsFunction function,int count){double sum=0;for(int i=0;i<count;i++)sum+=function.InvokeNumber1(i).GetValueOrDefault();return sum;}
