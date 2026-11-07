export {};
const buffer=new SharedArrayBuffer(8);const bytes=new Uint8Array(buffer);bytes.set([1,2,3,4,5,6,7,8]);const data=new DataView(buffer,2,4);
console.log("buffer",buffer.byteLength,buffer.maxByteLength,buffer.growable,buffer instanceof SharedArrayBuffer,buffer instanceof ArrayBuffer,ArrayBuffer.isView(buffer),Object.prototype.toString.call(buffer));
console.log("views",bytes.buffer===buffer,data.buffer===buffer,data.getUint8(0));data.setUint8(0,9);console.log(Array.from(bytes).join("|"));
const graph={buffer,bytes,data};const copied=structuredClone(graph);console.log("clone",copied.buffer!==buffer,copied.buffer instanceof SharedArrayBuffer,copied.bytes.buffer===copied.buffer,copied.data.buffer===copied.buffer);copied.bytes[0]=42;console.log("shared",bytes[0]);
const sliced=buffer.slice(1,5);new Uint8Array(sliced)[0]=99;console.log("slice",Array.from(new Uint8Array(sliced)).join("|"),bytes[1]);console.log("zero",buffer.slice(0,0).byteLength,new SharedArrayBuffer(0).byteLength);
class Derived extends SharedArrayBuffer{label="derived";}const derived=new Derived(4);const derivedSlice=derived.slice(0,2);console.log("derived",derived.label,derived instanceof SharedArrayBuffer,derivedSlice instanceof Derived,derivedSlice.byteLength);
for(const action of [()=>buffer.grow(9),()=>ArrayBuffer.prototype.slice.call(buffer),()=>ArrayBuffer.prototype.transfer.call(buffer),()=>structuredClone(buffer,{transfer:[buffer as any]}),()=>SharedArrayBuffer.prototype.slice.call(new ArrayBuffer(4))])try{action();}catch(error){console.log("invalid",(error as Error).name);}
console.log("metadata",SharedArrayBuffer.name,SharedArrayBuffer.length,SharedArrayBuffer.prototype.slice.name,SharedArrayBuffer.prototype.slice.length,SharedArrayBuffer[Symbol.species]===SharedArrayBuffer);
