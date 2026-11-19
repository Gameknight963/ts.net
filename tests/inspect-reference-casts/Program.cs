try
{
    if (Js.Cast<string>(null, "string") != null || Js.Cast<TsObject>(null, "object") != null || Js.Check<string>(null, "string") != null) throw new Exception("Null reference conversion failed");
    if (!ReferenceEquals(Js.Check<string>(Js.Undefined, "undefined"), Js.Undefined)) throw new Exception("Undefined reference conversion failed");
    void Reject(Action action) {
        try {action();} catch (InvalidCastException) {return;}
        throw new Exception("Invalid conversion succeeded");
    }
    Reject(() => Js.Cast<double>(null, "number"));
    Reject(() => Js.Cast<bool>(null, "boolean"));
    Reject(() => Js.Cast<double?>(null, "nullable number"));
    Reject(() => Js.Cast<double>("hello", "number"));
    Reject(() => Js.Cast<bool>(1d, "boolean"));
    Reject(() => Js.Check<string>(1d, "string"));
    Console.WriteLine("PASS null reference casts and invalid value conversions");
    var denseValues = new object[] {1d, Js.Undefined, null, "text"};
    var denseArray = new JsArray(denseValues);
    for (int i = 0; i < denseValues.Length; i++)
        if (!ReferenceEquals(Js.ArrayGetAt(denseArray, i), denseValues[i]))
            throw new Exception("Integer array read changed an own value");
    if (!ReferenceEquals(Js.ArrayGetAt(denseArray, 4), Js.Undefined) ||
        (string)Js.ArrayGetAt(new CustomReadArray(), 0) != "custom")
        throw new Exception("Integer array read bypassed its fallback");
    if ((string)Js.IndexGetNumberKey(new CustomReadArray(), 0) != "custom" ||
        (string)Js.IndexGetNumberKey("text", 1) != "e")
        throw new Exception("Numeric array read bypassed a subclass or string fallback");
    if ((string)Js.ArrayGetAt(new CustomGetArray(), 0) != "custom get" ||
        (string)Js.IndexGetNumberKey(new CustomGetArray(), 0) != "custom get")
        throw new Exception("An array subclass's property lookup override was bypassed");
    denseArray.SetOwnValue("undefined", "named");
    if ((string)Js.IndexGetNumberKey(denseArray, null) != "named")
        throw new Exception("Undefined array index changed its property lookup");
    foreach (var array in new JsArray[] {denseArray, new CustomLengthArray(uint.MaxValue)})
        foreach (int counter in new[] {-1, 0, 3, 4, int.MaxValue})
            if (Js.ArrayCounterBelowLength(array, counter) != (counter < Js.ArrayLength(array)))
                throw new Exception("Integer array bound comparison changed its result");
    int lengthReads = 0;
    var arrayLike = new JsDynamicObject();
    arrayLike.DefineAccessor("length", new JsFunction((FunctionBody)(_ => ++lengthReads == 1 ? 2.5d : 0d), null, "length", 0), 1);
    if (!Js.ArrayCounterBelowLength(arrayLike, 2) || Js.ArrayCounterBelowLength(arrayLike, 2) || lengthReads != 2)
        throw new Exception("Integer array bound comparison changed a non-array getter");
    for (int i = 0; i < 10000; i++) Js.ArrayGetAt(denseArray, i & 3);
    long denseBefore = GC.GetAllocatedBytesForCurrentThread();
    for (int i = 0; i < 10000; i++) Js.ArrayGetAt(denseArray, i & 3);
    if (GC.GetAllocatedBytesForCurrentThread() != denseBefore)
        throw new Exception("Integer array reads allocated");
    Console.WriteLine("PASS integer array reads preserve own values, subclass overrides, and zero allocations");
    foreach (object value in new object[] {true, false, Js.Undefined, null, 0d, 1, "true", new JsDynamicObject()})
    {
        bool? expected = null, actual = null;
        string expectedError = null, actualError = null;
        try {expected = Js.Cast<bool?>(value!, "boolean conversion");}
        catch (InvalidCastException error) {expectedError = error.Message;}
        try {actual = Js.CastBoolean(value!, "boolean conversion");}
        catch (InvalidCastException error) {actualError = error.Message;}
        if (actual != expected || actualError != expectedError)
            throw new Exception("Boolean conversion differs from nullable casts");
    }
    Console.WriteLine("PASS boolean conversions preserve values, undefined, null rejection and error messages");
    Reject(() => Js.Cast<string>(Js.Undefined, "string"));
    Reject(() => Js.Cast<double>(Js.Undefined, "number"));
    Reject(() => Js.Cast<bool>(Js.Undefined, "boolean"));
    Reject(() => Js.Cast<int>(Js.Undefined, "integer"));
    Reject(() => Js.Cast<int?>(null, "nullable integer"));
    Reject(() => Js.Cast<bool?>(null, "nullable boolean"));
    Reject(() => Js.Cast<double?>(1, "boxed integer"));
    if(Js.Cast<double?>(1d,"number")!=1 || Js.Cast<bool?>(true,"boolean")!=true || Js.Cast<int?>(3,"integer")!=3 || Js.Cast<decimal?>(4m,"decimal")!=4)
        throw new Exception("Nullable value conversion failed");
    if(!ReferenceEquals(Js.Cast<object>(Js.Undefined,"object"),Js.Undefined))throw new Exception("Undefined object identity changed");
    object negativeZero=-0d;
    if(BitConverter.DoubleToInt64Bits(Js.Cast<double?>(negativeZero,"negative zero").Value)!=long.MinValue)
        throw new Exception("Negative zero conversion changed");
    var warmup = System.Diagnostics.Stopwatch.StartNew();
    do {
        for(int i=0;i<1000;i++)if(NullableCasts(Js.Undefined)!=5)throw new Exception("Undefined nullable conversion failed");
    } while (warmup.ElapsedMilliseconds < 1000);
    long before=GC.GetAllocatedBytesForCurrentThread();
    for(int i=0;i<10000;i++)if(NullableCasts(Js.Undefined)!=5)throw new Exception("Undefined nullable conversion failed");
    long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
    if(bytes!=0)throw new Exception($"Undefined casts allocated {bytes} bytes");
    Console.WriteLine("PASS nullable values, undefined casts, and zero repeated cast allocations");
    foreach(int integer in new[]{int.MinValue,-1,0,1,255,256,int.MaxValue})
        if(IntegerBox(integer) is not double number || number!=integer)throw new Exception("Integer boxing changed numeric values");
    if(BitConverter.DoubleToInt64Bits((double)Js.BoxNumber(-0d))!=long.MinValue ||
        !ReferenceEquals(Js.BoxNumber(null),Js.Undefined))throw new Exception("Ordinary number boxing changed");
    for(int i=0;i<1000;i++)IntegerBox(i&255);
    before=GC.GetAllocatedBytesForCurrentThread();
    double total=0;
    for(int i=0;i<10000;i++)total+=(double)IntegerBox(i&255);
    bytes=GC.GetAllocatedBytesForCurrentThread()-before;
    if(bytes!=0 || total!=1273080)throw new Exception($"Cached integer boxing changed: {bytes} bytes, {total}");
    Console.WriteLine("PASS integer boxing values, negative zero, undefined and zero cached allocations");
    foreach(string name in new[]{"Uint8Array","Uint8ClampedArray"}) {
        var constructor=(JsFunction)Js.BuiltinNamespace(name);
        object view=constructor.Construct(new object[]{new JsArray(new object[]{0d,255d})});
        for(int i=0;i<1000;i++)TypedByteRead(view,i&1);
        before=GC.GetAllocatedBytesForCurrentThread();total=0;
        for(int i=0;i<10000;i++)total+=(double)TypedByteRead(view,i&1);
        bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        if(bytes!=0 || total!=1275000)throw new Exception($"Typed byte reads changed: {name}, {bytes} bytes, {total}");
    }
    Console.WriteLine("PASS boxed typed-byte reads preserve values without repeated allocation");

}
catch (Exception error) {Console.Error.WriteLine(error); Environment.ExitCode = 1;}

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static int NullableCasts(object value) =>
    (Js.Cast<double?>(value,"double").HasValue ? 0 : 1) +
    (Js.Cast<bool?>(value,"boolean").HasValue ? 0 : 1) +
    (Js.CastBoolean(value,"boolean").HasValue ? 0 : 1) +
    (Js.Cast<int?>(value,"integer").HasValue ? 0 : 1) +
    (Js.Cast<decimal?>(value,"decimal").HasValue ? 0 : 1);

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static object IntegerBox(int value) => Js.BoxInteger(value);

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static object TypedByteRead(object view, int index) => Js.IndexGetNumberKey(view,index);

sealed class CustomReadArray : JsArray
{
    internal CustomReadArray() : base(new object[] {1d}) { }
    protected override object Read(uint index) => "custom";
}

sealed class CustomLengthArray : JsArray
{
    readonly uint count;
    internal CustomLengthArray(uint count) : base(0) { this.count = count; }
    protected override uint Count => count;
}

sealed class CustomGetArray : JsArray
{
    internal CustomGetArray() : base(new object[] {1d}) { }
    public override object Get(string key) => "custom get";
}
