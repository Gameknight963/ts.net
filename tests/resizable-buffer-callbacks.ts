for(const method of ["map","filter"] as const) {
 const buffer=new ArrayBuffer(4,{maxByteLength:8});const array=new Uint8Array(buffer);array.set([1,2,3,4]);const seen:string[]=[];
 const result=array[method]((value,index)=>{seen.push(index+":"+value);if(index===0)buffer.resize(2);return method==="map"?value+1:1;});
 console.log(method,seen.join("|"),Array.from(result).join("|"));
}
const buffer=new ArrayBuffer(6,{maxByteLength:12});
const whole=new Uint8Array(buffer);whole.set([1,2,3,4,5,6]);
const views={buffer,tracking:new Uint8Array(buffer,2),fixed:new Uint8Array(buffer,2,2),data:new DataView(buffer,2)};
const copied=structuredClone(views);copied.buffer.resize(10);
console.log("clone",copied.tracking.buffer===copied.buffer,copied.tracking.length,copied.fixed.length,copied.data.byteLength);
const iterator=whole.values();console.log(iterator.next().value);buffer.resize(2);console.log(iterator.next().value,iterator.next().done);
const data=new DataView(buffer);
try{console.log(data.getUint8({valueOf(){buffer.resize(0);return 0;}} as any));}catch(error){console.log("coercion",(error as Error).name);}
for(const method of ["forEach","every","some","find","findIndex","findLast","findLastIndex","reduce","reduceRight"]){
 const buffer=new ArrayBuffer(4,{maxByteLength:8});const array=new Uint8Array(buffer);array.set([1,2,3,4]);const seen:string[]=[];
 const callback=(...args:any[])=>{const index=method.startsWith("reduce")?args[2]:args[1],value=method.startsWith("reduce")?args[1]:args[0];seen.push(index+":"+value);buffer.resize(1);return method==="every"?true:false;};
 console.log(method,(array as any)[method](callback,0),seen.join("|"));
}
const invalidBuffer=new ArrayBuffer(8,{maxByteLength:16});const invalidViews=[new Uint8Array(invalidBuffer,2,4),new DataView(invalidBuffer,2,4)];invalidBuffer.resize(1);
for(const view of invalidViews)try{structuredClone(view);}catch(error){console.log("invalid-clone",(error as Error).name);}
const sourceBuffer=new ArrayBuffer(8,{maxByteLength:16});const invalidSource=new Uint8Array(sourceBuffer,2,4);sourceBuffer.resize(1);try{new Uint8Array(8).set(invalidSource);}catch(error){console.log("invalid-source",(error as Error).name);}
