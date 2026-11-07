for(const resizable of [false,true])for(const method of ["transfer","transferToFixedLength"] as const)for(const length of [undefined,0,2,6]){
 const buffer=resizable?new ArrayBuffer(4,{maxByteLength:8}):new ArrayBuffer(4);const view=new Uint8Array(buffer,1,2),data=new DataView(buffer,1,2);new Uint8Array(buffer).set([1,2,3,4]);
 const result=buffer[method](length);
 console.log(resizable,method,length,buffer.detached,buffer.byteLength,buffer.maxByteLength,buffer.resizable,result.byteLength,result.maxByteLength,result.resizable,Array.from(new Uint8Array(result)).join("|"));
 console.log("view",view.length,view.byteLength,view.byteOffset,view[0],view.buffer===buffer,ArrayBuffer.isView(view));view[0]=9;console.log(view[0]);
 for(const action of [()=>data.byteLength,()=>data.byteOffset,()=>data.getUint8(0),()=>view.join(),()=>new Uint8Array(buffer),()=>new DataView(buffer),()=>buffer.slice(),()=>buffer.transfer(),()=>structuredClone(buffer)])try{action();}catch(error){console.log((error as Error).name);}
}
const source=new ArrayBuffer(4,{maxByteLength:6});try{source.transfer(7);}catch(error){console.log("limit",(error as Error).name,source.detached,source.byteLength);}
console.log("fixed",source.transferToFixedLength(9).byteLength,source.detached);
class Derived extends ArrayBuffer{}const derived=new Derived(3);const result=derived.transfer();console.log("intrinsic",result instanceof Derived,result instanceof ArrayBuffer);
for(const method of [ArrayBuffer.prototype.transfer,ArrayBuffer.prototype.transferToFixedLength])console.log("metadata",method.name,method.length);
const detached=new ArrayBuffer(4);const bytes=new Uint8Array(detached,2);detached.transfer();console.log("decoder",new TextDecoder().decode(detached),new TextDecoder().decode(bytes));console.log("encoder",JSON.stringify(new TextEncoder().encodeInto("hi",bytes)));console.log("random",crypto.getRandomValues(bytes)===bytes);
try{new Blob([detached]);}catch(error){console.log("blob",(error as Error).name);}
