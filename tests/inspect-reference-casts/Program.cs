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
}
catch (Exception error) {Console.Error.WriteLine(error); Environment.ExitCode = 1;}
