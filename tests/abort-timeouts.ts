export {};
const signal=AbortSignal.timeout(0);const combined=AbortSignal.any([signal]);
console.log("initial",signal.aborted,combined.aborted);
queueMicrotask(()=>console.log("microtask",signal.aborted));
signal.addEventListener("abort",event=>console.log("timeout",signal.aborted,signal.reason.name,signal.reason instanceof DOMException,event.isTrusted,combined.aborted));
combined.onabort=()=>console.log("combined-timeout",combined.reason===signal.reason);
setTimeout(()=>{console.log("settled",signal.aborted,combined.aborted);try{signal.throwIfAborted();}catch(error){console.log("throw",error===signal.reason);}},25);
for(const value of [-1,1.5,NaN,Infinity,4294967296])try{AbortSignal.timeout(value);}catch(error){console.log("invalid",(error as Error).name);}
for(const value of [undefined,null,"1",false])try{AbortSignal.timeout(value as any);}catch(error){console.log("type",(error as Error).name);}
