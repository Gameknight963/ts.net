using System.Reflection;

_ = typeof(Js).Assembly;
var functions = Assembly.LoadFile(Path.GetFullPath(args[0]));
var types = functions.GetTypes().Where(t => t.BaseType == typeof(JsFunction)).ToArray();
foreach (int arity in new[] { 0, 1, 2 }) {
    if (!types.Any(t => t.IsSealed && t.GetMethod("InvokeNumber" + arity)!.DeclaringType == t))
        throw new Exception($"Missing generated numeric callback class for arity {arity}");
}
foreach (var type in types.Where(type => Enumerable.Range(0,3).Any(arity => type.GetMethod("InvokeNumber"+arity)!.DeclaringType==type)))
    foreach(int arity in new[]{0,1,2})
        if(type.GetMethod("InvokeNumber"+arity)!.DeclaringType!=type)
            throw new Exception($"Numeric callback lacks direct handling for {arity} arguments: {type.Name}");
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

FunctionBody nativeBody = static values => (double)values.Length;
object[] numericDelegates = {
    (RawNumberFunction0)(() => 11),
    (RawNumberFunction1)(value => value + 1),
    (RawNumberFunction2)((first,second) => first*10+second),
    (NumberFunction0)(() => 11),
    (NumberFunction1)(value => value ?? 7),
    (NumberFunction2)((first,second) => (first ?? 7)*10+(second ?? 9))
};
double[][] expectedByDelegate = {
    new[]{11d,11,11}, new[]{double.NaN,3,3}, new[]{double.NaN,double.NaN,23},
    new[]{11d,11,11}, new[]{7d,2,2}, new[]{79d,29,23}
};
for (int kind=0;kind<numericDelegates.Length;kind++) {
    var numberFunction = new JsFunction(nativeBody,numericDelegates[kind],"native",2);
    double?[] results = {numberFunction.InvokeNumber0(),numberFunction.InvokeNumber1(2),numberFunction.InvokeNumber2(2,3)};
    for(int arity=0;arity<3;arity++)
        if(!results[arity].HasValue || !results[arity].Value.Equals(expectedByDelegate[kind][arity]))
            throw new Exception($"Numeric delegate dispatch mismatch: {kind}, {arity}");
}
var genericFunction = new JsFunction(nativeBody,null,"generic",0);
if(genericFunction.InvokeNumber0()!=0 || genericFunction.InvokeNumber1(2)!=1 || genericFunction.InvokeNumber2(2,3)!=2)
    throw new Exception("Generic numeric fallback changed argument counts");
var nullableFunction = new JsFunction(nativeBody,(NumberFunction1)(value=>value),"nullable",1);
var rawFunction = new JsFunction(nativeBody,numericDelegates[1],"raw",1);
if(nullableFunction.InvokeNumber0()!=null || nullableFunction.InvokeNumber1(null)!=null || !double.IsNaN(rawFunction.InvokeNumber1(null).GetValueOrDefault()))
    throw new Exception("Missing numeric arguments changed");
Console.WriteLine("PASS native numeric delegates, missing arguments, and generic call fallbacks");
