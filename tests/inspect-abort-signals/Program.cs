using System.Runtime.CompilerServices;
try {
var controllerConstructor=(JsFunction)Js.BuiltinNamespace("AbortController");var signalConstructor=(TsObject)Js.BuiltinNamespace("AbortSignal");
var any=(JsFunction)signalConstructor.Get("any");
var controller=(TsObject)controllerConstructor.Construct(Array.Empty<object>());var root=(TsObject)controller.Get("signal");
TsObject tail=root;for(int index=0;index<10000;index++)tail=(TsObject)any.InvokeWithReceiver(signalConstructor,new object[]{new JsArray(new object[]{tail})});
GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
((JsFunction)controller.Get("abort")).InvokeWithReceiver(controller,new object[]{"reason"});
if(!(bool)tail.Get("aborted") || (string)tail.Get("reason")!="reason")throw new Exception("Composite chain lost a source during GC");
GC.KeepAlive(controller);GC.KeepAlive(tail);
var owner=(TsObject)controllerConstructor.Construct(Array.Empty<object>());var signal=(TsObject)owner.Get("signal");
var weak=RemovedTarget(signal);GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
if(weak.TryGetTarget(out _))throw new Exception("Removed listener retained its event target through the signal");
var abandoned=UnusedComposite(signal,false);var removedComposite=UnusedComposite(signal,true);
GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
if(abandoned.TryGetTarget(out _) || removedComposite.TryGetTarget(out _))throw new Exception("Unobserved composites retained through live sources");
int observed=0;ObservedComposite(signal,()=>observed++);GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
((JsFunction)owner.Get("abort")).InvokeWithReceiver(owner,Array.Empty<object>());
if(observed!=1)throw new Exception("A dependent signal with a listener was collected before abort");
GC.KeepAlive(owner);
var failingOwner=(TsObject)controllerConstructor.Construct(Array.Empty<object>());var failingSource=(TsObject)failingOwner.Get("signal");
var dependent=(TsObject)any.InvokeWithReceiver(signalConstructor,new object[]{new JsArray(new object[]{failingSource})});int delivered=0;
var failure=new JsFunction((FunctionBody)(static _=>throw new InvalidOperationException("abort listener failure")),null,"failure",1);
var delivery=new JsFunction((FunctionBody)(_=>{delivered++;return Js.Undefined;}),null,"delivery",1);
((JsFunction)failingSource.Get("addEventListener")).InvokeWithReceiver(failingSource,new object[]{"abort",failure});
((JsFunction)dependent.Get("addEventListener")).InvokeWithReceiver(dependent,new object[]{"abort",delivery});
bool propagated=false;try{((JsFunction)failingOwner.Get("abort")).InvokeWithReceiver(failingOwner,Array.Empty<object>());}catch(InvalidOperationException error)when(error.Message=="abort listener failure"){propagated=true;}
if(!propagated || !(bool)dependent.Get("aborted") || delivered!=1)throw new Exception("Listener exception prevented dependent cancellation");
bool closed=false;
var iterator=new JsDynamicObject();
iterator.DefineOwn("next",new JsFunction((FunctionBody)(static _=>Result(new JsDynamicObject(),false)),null,"next",0));
iterator.DefineOwn("return",new JsFunction((FunctionBody)(_=>{closed=true;return Result(Js.Undefined,true);}),null,"return",0));
var iterable=new JsDynamicObject();var symbol=((TsObject)Js.BuiltinNamespace("Symbol")).Get("iterator");
iterable.DefineOwn(Js.PropertyKey(symbol),new JsFunction((FunctionBody)(_=>iterator),null,"",0));
try{any.InvokeWithReceiver(signalConstructor,new object[]{iterable});}catch(InvalidOperationException){}
if(!closed)throw new Exception("Invalid signal iteration did not close the iterator");
Console.WriteLine("PASS linked signal GC lifetimes, observed and unobserved composites, listener cleanup, and iterator closing");
static TsObject Result(object value,bool done){var result=new JsDynamicObject();result.DefineOwn("value",value);result.DefineOwn("done",done);return result;}
[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference<TsObject> RemovedTarget(TsObject signal){
 var target=(TsObject)((JsFunction)Js.BuiltinNamespace("EventTarget")).Construct(Array.Empty<object>());
 var callback=new JsFunction((FunctionBody)(static _=>Js.Undefined),null,"listener",1);var options=new JsDynamicObject();options.DefineOwn("signal",signal);
 ((JsFunction)target.Get("addEventListener")).InvokeWithReceiver(target,new object[]{"event",callback,options});
 ((JsFunction)target.Get("removeEventListener")).InvokeWithReceiver(target,new object[]{"event",callback});
 return new WeakReference<TsObject>(target);
}

[MethodImpl(MethodImplOptions.NoInlining)]
static void ObservedComposite(TsObject source,Action callback){
 var constructor=(TsObject)Js.BuiltinNamespace("AbortSignal");
 TsObject signal=source;
 for(int index=0;index<2000;index++)signal=(TsObject)((JsFunction)constructor.Get("any")).InvokeWithReceiver(constructor,new object[]{new JsArray(new object[]{signal})});
 var handler=new JsFunction((FunctionBody)(_=>{callback();return Js.Undefined;}),null,"listener",1);
 ((JsFunction)signal.Get("addEventListener")).InvokeWithReceiver(signal,new object[]{"abort",handler});
}

[MethodImpl(MethodImplOptions.NoInlining)]
static WeakReference<TsObject> UnusedComposite(TsObject source,bool observe){
 var constructor=(TsObject)Js.BuiltinNamespace("AbortSignal");
 var signal=(TsObject)((JsFunction)constructor.Get("any")).InvokeWithReceiver(constructor,new object[]{new JsArray(new object[]{source})});
 if(observe){
  var handler=new JsFunction((FunctionBody)(static _=>Js.Undefined),null,"listener",1);
  ((JsFunction)signal.Get("addEventListener")).InvokeWithReceiver(signal,new object[]{"abort",handler});
  ((JsFunction)signal.Get("removeEventListener")).InvokeWithReceiver(signal,new object[]{"abort",handler});
 }
 return new WeakReference<TsObject>(signal);
}

} catch(Exception error){Console.Error.WriteLine(error);Environment.ExitCode=1;}
