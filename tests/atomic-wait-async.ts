export {};
async function main():Promise<void>{
 const ints=new Int32Array(new SharedArrayBuffer(16));
 const mismatch=Atomics.waitAsync(ints,0,1);console.log("mismatch",mismatch.async,mismatch.value,Object.keys(mismatch).join(","));
 const zero=Atomics.waitAsync(ints,0,0,0);console.log("zero",zero.async,zero.value);
 const pending=Atomics.waitAsync(ints,0,0);console.log("pending",pending.async,pending.value instanceof Promise);
 if(pending.async){const promise=pending.value;console.log("notify",Atomics.notify(ints,0,1));console.log("resolved",await promise);}
 const keep=setTimeout(()=>console.log("unexpected timeout"),1000);
 const timed=Atomics.waitAsync(ints,0,0,2);if(timed.async)console.log("timeout",await timed.value);clearTimeout(keep);
 const alias=new Int32Array(ints.buffer,4,1);const offset=Atomics.waitAsync(ints,1,0);
 console.log("alias notify",Atomics.notify(alias,0,1));if(offset.async)console.log("alias",await offset.value);
 const wide=new BigInt64Array(ints.buffer);const cross=Atomics.waitAsync(wide,0,0n);
 console.log("cross notify",Atomics.notify(ints,0,1));if(cross.async)console.log("cross",await cross.value);
 const order:string[]=[];const ordered=Atomics.waitAsync(ints,0,0);
 if(ordered.async){const done=ordered.value.then(value=>{order.push(value);});Atomics.notify(ints,0);await Promise.resolve().then(()=>{order.push("microtask");});await done;console.log("order",order.join(","));}
 const batch:Promise<string>[]=[];
 for(let index=0;index<10;index++){const result=Atomics.waitAsync(ints,0,0);if(result.async)batch.push(result.value);}
 console.log("batch notify",Atomics.notify(ints,0,3),Atomics.notify(ints,0));console.log("batch",(await Promise.all(batch)).join(","));
 const fn:any=Atomics.waitAsync;console.log("metadata",fn.name,fn.length,Object.getOwnPropertyDescriptor(Atomics,"waitAsync")!.enumerable);
 for(const input of [new Int32Array(1),new Uint32Array(ints.buffer),new Int32Array(new SharedArrayBuffer(0))])try{fn(input,0,0,0);}catch(error){console.log("invalid array",(error as Error).name);}
 for(const timeout of [-1,0]){const result=fn(ints,0,0,timeout);console.log("immediate",timeout,result.async,result.value);}
 try{fn(wide,0,0,0);}catch(error){console.log("bigint expected",(error as Error).name);}
 try{new fn();}catch(error){console.log("constructor",(error as Error).name);}
 const last=Atomics.waitAsync(ints,0,0);console.log("unreferenced",last.async);
}
main().catch(error=>console.log("failure",String(error)));
