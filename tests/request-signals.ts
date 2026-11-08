export {};
const controller=new AbortController();const request=new Request("https://example.com/",{signal:controller.signal});const clone=request.clone();
console.log("identity",request.signal!==controller.signal,request.signal===request.signal,clone.signal!==request.signal,request.signal.aborted,clone.signal.aborted);
let effects="";request.signal.addEventListener("abort",()=>{effects+="r";});clone.signal.addEventListener("abort",()=>{effects+="c";});
const reason={kind:"stop"};controller.abort(reason);console.log("follow",request.signal.aborted,clone.signal.aborted,request.signal.reason===reason,clone.signal.reason===reason,effects);
const inherited=new Request(request);const reset=new Request(request,{signal:null});console.log("aborted input",inherited.signal.aborted,inherited.signal.reason===reason,reset.signal.aborted,reset.signal.reason);
const defaults=new Request("https://example.com/");console.log("default signal",defaults.signal instanceof AbortSignal,defaults.signal.aborted,defaults.signal.reason);
try{request.signal.throwIfAborted();}catch(error){console.log("reason identity",error===reason);}
const other=new AbortController();const overridden=new Request(request,{signal:other.signal});console.log("override",overridden.signal.aborted);other.abort("other");console.log("new source",overridden.signal.reason);
