const buffer=new ArrayBuffer(8,{maxByteLength:16});
const tracking=new Uint8Array(buffer),fixed=new Uint8Array(buffer,2,4),offset=new Uint8Array(buffer,2);
const view=new DataView(buffer,2),fixedView=new DataView(buffer,2,4);
tracking.set([1,2,3,4,5,6,7,8]);
function show(){console.log(buffer.byteLength,buffer.maxByteLength,buffer.resizable,tracking.length,fixed.length,fixed.byteOffset,offset.length);console.log(Array.from(tracking).join("|"));for(const data of [view,fixedView])try{console.log("view",data.byteLength,data.byteOffset);}catch(error){console.log("view",(error as Error).name);}}
show();buffer.resize(12);show();buffer.resize(4);show();buffer.resize(1);show();buffer.resize(8);show();
for(const size of [-1,17,Infinity])try{buffer.resize(size);}catch(error){console.log("resize",(error as Error).name);}
const regular=new ArrayBuffer(3);console.log(regular.resizable,regular.maxByteLength);try{regular.resize(2);}catch(error){console.log((error as Error).name);}
const copied=structuredClone(buffer);console.log(copied.resizable,copied.maxByteLength,copied.byteLength);
const sub=tracking.subarray(2);const subFixed=tracking.subarray(2,4);
buffer.resize(12);console.log("sub",sub.length,subFixed.length);buffer.resize(1);
for(const data of [fixed,offset]) {
 console.log("oob",data[0],data.length,data.byteOffset,data.byteLength);
 try{data.join();}catch(error){console.log("method",(error as Error).name);}
}
buffer.resize(8);console.log("recover",fixed.length,offset.length,sub.length,subFixed.length);
const unaligned=new ArrayBuffer(5,{maxByteLength:12});const words=new Uint16Array(unaligned);console.log("alignment",words.length);unaligned.resize(7);console.log(words.length);
class GrowingBuffer extends ArrayBuffer { label="growing"; }
const derived=new GrowingBuffer(2,{maxByteLength:8});derived.resize(4);console.log("derived",derived.label,derived.byteLength,derived instanceof GrowingBuffer,derived instanceof ArrayBuffer);
let maximumReads=0;const options={get maxByteLength(){maximumReads++;return 8;}};const observed=new ArrayBuffer(2,options);console.log("options",maximumReads,observed.maxByteLength);
for(const [length,maximum] of [[3,2],[0,-1],[0,Infinity]])try{new ArrayBuffer(length,{maxByteLength:maximum});}catch(error){console.log("maximum",(error as Error).name);}
console.log("metadata",ArrayBuffer.prototype.resize.name,ArrayBuffer.prototype.resize.length);
