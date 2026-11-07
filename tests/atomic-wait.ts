export {};
for(const big of [false,true]){
 const values:any=big?new BigInt64Array(new SharedArrayBuffer(8)):new Int32Array(new SharedArrayBuffer(8));
 console.log("wait",big,(Atomics.wait as any)(values,0,big?1n:1,Infinity),(Atomics.wait as any)(values,0,big?0n:0,0),(Atomics.wait as any)(values,0,big?0n:0,1));
 console.log("notify",Atomics.notify(values,0),Atomics.notify(values,0,0),Atomics.notify(values,0,-1));
}
console.log("unshared-notify",Atomics.notify(new Int32Array(1),0));try{Atomics.wait(new Int32Array(1),0,0,0);}catch(error){console.log((error as Error).name);}
for(const value of [new Uint32Array(1),new BigUint64Array(1),new Float32Array(1)])try{Atomics.notify(value as any,0);}catch(error){console.log("type",(error as Error).name);}
console.log("metadata",Atomics.wait.name,Atomics.wait.length,Atomics.notify.name,Atomics.notify.length);
