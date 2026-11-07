export {};
const controller=new AbortController(),signal=controller.signal;
console.log("initial",signal===controller.signal,signal.aborted,signal.reason,signal.onabort,signal instanceof AbortSignal,signal instanceof EventTarget);
signal.throwIfAborted();
const reason={message:"stop"};
signal.addEventListener("abort",function(this:AbortSignal,event){console.log("listener",this===signal,event.target===signal,event.type,event.bubbles,event.cancelable,event.isTrusted,signal.aborted,signal.reason===reason);});
signal.onabort=function(event){console.log("handler",this===signal,event.target===signal);};
controller.abort(reason);controller.abort("ignored");console.log("final",signal.aborted,signal.reason===reason);
try{signal.throwIfAborted();}catch(error){console.log("throw",error===reason);}
const normal=new AbortController();normal.abort();console.log("default",normal.signal.reason.name,normal.signal.reason instanceof DOMException,normal.signal.reason instanceof Error);try{normal.signal.throwIfAborted();}catch(error){console.log(error===normal.signal.reason);}
for(const reason of [undefined,null,false,0,""]){const value=AbortSignal.abort(reason);console.log("static",value.aborted,reason===undefined?value.reason.name:value.reason);}
const target=new EventTarget(),owner=new AbortController();let calls=0;const listener=()=>{calls++;};target.addEventListener("x",listener,{signal:owner.signal});target.dispatchEvent(new Event("x"));owner.abort();target.dispatchEvent(new Event("x"));target.addEventListener("x",listener,{signal:owner.signal});target.dispatchEvent(new Event("x"));console.log("remove",calls);
const first=new AbortController(),second=new AbortController();const combined=AbortSignal.any([first.signal,second.signal,first.signal]);combined.addEventListener("abort",()=>console.log("combined-event",combined.reason));second.abort("second");first.abort("first");console.log("combined",combined.aborted,combined.reason,AbortSignal.any([]).aborted,AbortSignal.any([first.signal,second.signal]).reason);
class Controller extends AbortController{value=7;}const derived=new Controller();derived.abort(3);console.log("derived",derived.value,derived instanceof AbortController,derived.signal.reason);
for(const method of [AbortController.prototype.abort,AbortSignal.prototype.throwIfAborted,AbortSignal.abort,AbortSignal.any,AbortSignal.timeout])console.log("metadata",method.name,method.length);
console.log("globals",globalThis.AbortController===AbortController,globalThis.AbortSignal===AbortSignal);
