using System.Collections;
using System.Reflection;

try
{
    const BindingFlags flags = BindingFlags.Static | BindingFlags.NonPublic;
    var registry = typeof(Js).GetField("objectUrls", flags)!;
    int Count() => (registry.GetValue(null) as IDictionary)?.Count ?? 0;
    void Create() {
        var url = (TsObject)Js.BuiltinNamespace("URL");
        object blob = ((JsFunction)Js.BuiltinNamespace("Blob")).Construct(Array.Empty<object>());
        Js.CallMethod(url.Get("createObjectURL"), url, new[] {blob});
    }
    int status = Js.Run(() => {
        Create();
        if (Count() != 1) throw new Exception("Blob registration failed");
        if (Task.Run(Count).GetAwaiter().GetResult() != 0) throw new Exception("Blob registry escaped to another thread");
    });
    if (status != 0 || Count() != 0) throw new Exception("Successful entry retained blob URLs");
    var previous = Console.Error;
    try {
        Console.SetError(new StringWriter());
        status = Js.Run(() => {Create(); throw new Exception("entry failed");});
    } finally {Console.SetError(previous);}
    if (status == 0 || Count() != 0) throw new Exception("Failed entry retained blob URLs");
    Console.WriteLine("PASS blob URL registries are thread-local and released after successful and failed entries");
    var range = typeof(Js).GetMethod("BlobFetchRange", flags)!;
    foreach (var item in new[] {("bytes=6-", (6, 5)), ("bytes=-999", (0, 11)), ("bytes=-0", (11, 0))})
        if (((int, int))range.Invoke(null, new object[] {item.Item1, 11})! != item.Item2) throw new Exception("Incorrect blob range: " + item.Item1);
    Console.WriteLine("PASS open-ended and boundary blob ranges");
}
catch (Exception error) {Console.Error.WriteLine(error); Environment.ExitCode = 1;}
