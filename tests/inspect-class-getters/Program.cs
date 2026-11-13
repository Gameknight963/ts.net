using System;
using System.Runtime.CompilerServices;
foreach (bool read in new[] {false, true}) {
    var state = Setup(read);
    if (!state.Owner.Prototype.DeleteOwn("value")) throw new Exception("Getter deletion failed");
    for (int i=0;i<3;i++) {GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();}
    if (state.Getter.TryGetTarget(out _)) throw new Exception("Getter cache retains a deleted function");
    GC.KeepAlive(state.Owner); GC.KeepAlive(state.Instance);
    Console.WriteLine("PASS deleted getter is collectible with class alive; read=" + read);
}
var sharedState = Setup(false);
var shared = new JsFunction((FunctionBody)(static _ => 17d), null, "value", 0);
Js.DefineSimpleClassGetter(sharedState.Owner, "value", shared, 1);
if (Js.MatchesSimpleClassGetter(sharedState.Instance, 1)) throw new Exception("Shared delegate type was accepted as a source getter");
Console.WriteLine("PASS shared runtime function types use the ordinary getter path");

[MethodImpl(MethodImplOptions.NoInlining)]
static (JsClassFunction Owner, object Instance, WeakReference<JsFunction> Getter) Setup(bool read) {
    var fields = new JsFunction((FunctionBody)(static _ => Js.Undefined), null, "", 0);
    var owner = new JsClassFunction("Holder", (FunctionBody)(static _ => new Sample()), fields, null, null, false);
    var getter = new SourceGetter();
    Js.DefineSimpleClassGetter(owner, "value", getter, 0);
    object instance = owner.Construct(Array.Empty<object>());
    if (read && !Js.MatchesSimpleClassGetter(instance, 0)) throw new Exception("Default getter was not recognized");
    return (owner, instance, new WeakReference<JsFunction>(getter));
}
sealed class Sample : JsClassInstance { }
sealed class SourceGetter() : JsFunction((FunctionBody)(static _ => 13d), null, "value", 0) { }
