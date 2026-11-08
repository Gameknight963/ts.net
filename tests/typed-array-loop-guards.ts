export {};
function optimized(values:Float64Array):number{let result=0;for(let i=0;i<values.length;i++)result+=values[i];return result;}
function sum(values:ArrayLike<number>):number{return optimized(values as Float64Array);}
console.log("fixed",sum(new Float64Array([1,2,3])),sum(new Float32Array([1.5,2.5])),sum(new Uint8Array([4,5,6])),sum([7,8]));
const storage=new Float64Array([0,1,2,3,0]);console.log("offset",sum(storage.subarray(1,4)),sum(storage.subarray(0,3)),sum(new Float64Array()));
let reads=0;const object={0:1,1:2,2:3,get length(){reads++;return 3;}};
console.log("array-like getter",sum(object),reads);
const custom=new Float64Array([2,4,6]);reads=0;Object.setPrototypeOf(custom,{get length(){reads++;return 3;}});
console.log("typed getter",sum(custom),reads);
const borrowed=new Float64Array([1,2]);const prototype=Object.getPrototypeOf(borrowed);
const getter=Object.getOwnPropertyDescriptor(prototype,"byteLength")?.get??Object.getOwnPropertyDescriptor(Object.getPrototypeOf(prototype),"byteLength")!.get!;
Object.setPrototypeOf(borrowed,Object.create(prototype,{length:{get:getter}}));console.log("borrowed length",sum(borrowed));
function replace(first:Float64Array,next:Float64Array){let values=first,result=0;for(let i=0;i<values.length;i++){result+=values[i];if(i===0)values=next;}return result;}
console.log("replacement",replace(new Float64Array([1,2,3]),new Float64Array([100,200])));
function shrink(values:Float64Array,buffer:ArrayBuffer){let result=0;for(let i=0;i<values.length;i++){result+=values[i];if(i===0)buffer.resize(8);}return result;}
const buffer=new ArrayBuffer(24,{maxByteLength:32}),values=new Float64Array(buffer);values.set([3,4,5]);console.log("shrink",shrink(values,buffer));
function detach(values:Float64Array,buffer:ArrayBuffer){let result=0;for(let i=0;i<values.length;i++){result+=values[i];if(i===0)buffer.transfer();}return result;}
const moved=new ArrayBuffer(24),view=new Float64Array(moved);view.set([7,8,9]);console.log("detach",detach(view,moved));
const growable=new ArrayBuffer(24,{maxByteLength:32}),tracked=new Float64Array(growable);tracked.set([1,2,3]);console.log("read-only resizable",sum(tracked));

function sumLocal(values:Float64Array){let result=0;for(let i=0;i<values.length;i++){const value=values[i];result+=value;}return result;}
console.log("local binding",sumLocal(new Float64Array([2,3,4])));

const shadowed = new Float64Array([10, 20, 30]);
const direct = Object.getPrototypeOf(shadowed);
Object.defineProperty(direct, 'length', {value: 1, configurable: true});
try {console.log('prototype data shadow', sum(shadowed));}
finally {Reflect.deleteProperty(direct, 'length');}
console.log('prototype data restored', sum(shadowed));
