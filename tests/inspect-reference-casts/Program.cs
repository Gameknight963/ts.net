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
    for(int i=0;i<1000;i++)if(NullableCasts(Js.Undefined)!=4)throw new Exception("Undefined nullable conversion failed");
    long before=GC.GetAllocatedBytesForCurrentThread();
    for(int i=0;i<10000;i++)if(NullableCasts(Js.Undefined)!=4)throw new Exception("Undefined nullable conversion failed");
    long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
    if(bytes!=0)throw new Exception($"Undefined casts allocated {bytes} bytes");
    Console.WriteLine("PASS nullable values, undefined casts, and zero repeated cast allocations");

}
catch (Exception error) {Console.Error.WriteLine(error); Environment.ExitCode = 1;}

[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
static int NullableCasts(object value) =>
    (Js.Cast<double?>(value,"double").HasValue ? 0 : 1) +
    (Js.Cast<bool?>(value,"boolean").HasValue ? 0 : 1) +
    (Js.Cast<int?>(value,"integer").HasValue ? 0 : 1) +
    (Js.Cast<decimal?>(value,"decimal").HasValue ? 0 : 1);
