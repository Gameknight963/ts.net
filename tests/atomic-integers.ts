export {};
for(const shared of [false,true])for(const Constructor of [Int8Array,Uint8Array,Int16Array,Uint16Array,Int32Array,Uint32Array,BigInt64Array,BigUint64Array]){
 const array:any=new (Constructor as any)(shared?new SharedArrayBuffer(8):new ArrayBuffer(8));const big=Constructor===BigInt64Array||Constructor===BigUint64Array;
 const values:any[]=big?[0n,1n,-1n,(1n<<64n)+1n,-(1n<<80n)]:[0,-0,1,257,-257,4294967297,1e100,NaN,Infinity,-Infinity,1.9];
 for(const operation of ["add","sub","and","or","xor","exchange","store","compareExchange"]){
  const method=(Atomics as any)[operation];
  for(const value of values){array[0]=big?-1n:-1;const result=operation==="compareExchange"?method(array,0,value,big?9n:9):method(array,0,value);console.log(shared,Constructor.name,operation,String(value),String(result),String(Atomics.load(array,0)));}
 }
}
const view=new Int32Array(new SharedArrayBuffer(16),4,2);console.log("offset",Atomics.store(view,1,7),Atomics.add(view,1,3),Atomics.load(view,1));
for(const array of [new Float32Array(2),new Uint8ClampedArray(2),new DataView(new ArrayBuffer(4)),{}])try{Atomics.load(array as any,0);}catch(error){console.log("invalid-type",(error as Error).name);}
for(const index of [-1,2,Infinity])try{Atomics.load(new Int32Array(2),index);}catch(error){console.log("invalid-index",(error as Error).name);}
console.log("metadata",Atomics.add.name,Atomics.add.length,Atomics.load.length,Atomics.store.length,Atomics.compareExchange.length,Object.prototype.toString.call(Atomics));
