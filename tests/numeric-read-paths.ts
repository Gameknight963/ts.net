export {};
function read(values:ArrayLike<number>,index:number):number{return values[index];}
function length<T>(values:ArrayLike<T>):number{return values.length;}
for(const Constructor of [Int8Array,Uint8Array,Uint8ClampedArray,Int16Array,Uint16Array,Int32Array,Uint32Array,Float32Array,Float64Array]){
 const values=new Constructor([1,2,3]);
 console.log("typed",Constructor.name,read(values,0),read(values,2),read(values,-1),read(values,1.5),read(values,3),read(values,NaN),length(values));
}
console.log("ordinary",read([1,2,3],1),read({0:5,length:1},0),read([],0),length([1,2]),length("abc"));
const indexes:number[]=[];const record:any={length:1,undefined:17,NaN:18,Infinity:19,"-1":20};
console.log("keys",read(record,indexes[0]),read(record,NaN),read(record,Infinity),read(record,-1));
const values=new Float64Array([4,5,6]);const prototype=Object.getPrototypeOf(values);
Object.setPrototypeOf(values,{get length(){return 1;}});
console.log("custom prototype",length(values),read(values,2));
Object.setPrototypeOf(values,prototype);
console.log("restored",length(values),read(values,2));
const byteLength=Object.getOwnPropertyDescriptor(prototype,"byteLength")?.get ?? Object.getOwnPropertyDescriptor(Object.getPrototypeOf(prototype),"byteLength")!.get!;
const alternate=Object.create(prototype,{length:{get:byteLength}});Object.setPrototypeOf(values,alternate);
console.log("borrowed getter",length(values));
const buffer=new ArrayBuffer(16,{maxByteLength:32}),tracking=new Float64Array(buffer);tracking[0]=9;tracking[1]=10;
console.log("resizable",length(tracking),read(tracking,1));buffer.resize(8);console.log("shrink",length(tracking),read(tracking,1));buffer.resize(24);console.log("grow",length(tracking),read(tracking,2));
buffer.transfer();console.log("detached",length(tracking),read(tracking,0));
