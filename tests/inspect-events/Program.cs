var targetConstructor=(JsFunction)Js.BuiltinNamespace("EventTarget");var eventConstructor=(JsFunction)Js.BuiltinNamespace("Event");
var target=(TsObject)targetConstructor.Construct(Array.Empty<object>());var value=(TsObject)eventConstructor.Construct(new object[]{"test"});
var callback=new JsFunction((FunctionBody)(static _=>throw new InvalidOperationException("listener failure")),null,"listener",1);
var add=(JsFunction)target.Get("addEventListener");var remove=(JsFunction)target.Get("removeEventListener");var dispatch=(JsFunction)target.Get("dispatchEvent");
add.InvokeWithReceiver(target,new object[]{"test",callback});
bool propagated=false;try{dispatch.InvokeWithReceiver(target,new object[]{value});}catch(InvalidOperationException error)when(error.Message=="listener failure"){propagated=true;}
if(!propagated || value.Get("currentTarget")!=null || (double)value.Get("eventPhase")!=0)throw new Exception("Listener failure left invalid dispatch state");
remove.InvokeWithReceiver(target,new object[]{"test",callback});
if(!(bool)dispatch.InvokeWithReceiver(target,new object[]{value}))throw new Exception("Event could not be reused after a listener failure");
Console.WriteLine("PASS listener exceptions propagate and dispatch state resets for event reuse");
var nested=new JsFunction((FunctionBody)(_=>{
 bool rejected=false;
 try{dispatch.InvokeWithReceiver(target,new object[]{value});}
 catch(JsThrownException error){rejected=error.Value is TsObject data && (string)data.Get("name")=="InvalidStateError";}
 if(!rejected)throw new Exception("Reentrant dispatch was not rejected");
 return Js.Undefined;
}),null,"nested",1);
add.InvokeWithReceiver(target,new object[]{"test",nested});dispatch.InvokeWithReceiver(target,new object[]{value});
Console.WriteLine("PASS reentrant dispatch reports InvalidStateError");
