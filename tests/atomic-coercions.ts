export {};
const values=new Int32Array(new SharedArrayBuffer(8));let order="";
const index={valueOf(){order+="index|";return 0;}},expected={valueOf(){order+="expected|";values[0]=2;return 2;}},replacement={valueOf(){order+="replacement|";values[0]=3;return 9;}};
console.log("order",Atomics.compareExchange(values,index as any,expected as any,replacement as any),values[0],order);
console.log("store",Object.is(Atomics.store(values,0,-0),0),Atomics.store(values,0,Infinity),Atomics.load(values,0));
const big=new BigUint64Array(new SharedArrayBuffer(8));console.log("big",String(Atomics.add(big,0,"18446744073709551617" as any)),String(big[0]));try{Atomics.add(big,0,1 as any);}catch(error){console.log("big-type",(error as Error).name);}
const buffer=new ArrayBuffer(8,{maxByteLength:16}),fixed=new Int32Array(buffer,0,2);try{Atomics.add(fixed,0,{valueOf(){buffer.resize(0);return 1;}} as any);}catch(error){console.log("resize",(error as Error).name);}
const detached=new ArrayBuffer(4),view=new Int32Array(detached);try{Atomics.store(view,0,{valueOf(){detached.transfer();return 1;}} as any);}catch(error){console.log("detach",(error as Error).name);}
