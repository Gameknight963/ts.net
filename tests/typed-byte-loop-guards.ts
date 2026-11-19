export {};
function prefix(values:any):number {let sum=0;for(let i=0;i<3;i++)sum+=values[i] as number;return sum;}
function lengthSum(values:any):number {let sum=0;for(let i=0;i<values.length;i++)sum+=values[i] as number;return sum;}
function fromOne(values:any):number {let sum=0;for(let i=1;i<3;i++)sum+=values[i] as number;return sum;}
for(const values of [new Uint8Array([1,2,3,4]),new Uint8ClampedArray([1,2,3,4]),new Float64Array([1,2,3,4]),[1,2,3,4]]) {
 console.log('bounds',prefix(values),lengthSum(values),fromOne(values));
}
console.log('short',prefix(new Uint8Array([1,2])),prefix([1,2]),lengthSum(new Uint8Array()));
const storage=new Uint8Array([9,1,2,3,9]);
console.log('views',prefix(storage.subarray(1,4)),prefix(storage.subarray(0,3)));
let reads=0;
const shadowed=new Uint8Array([1,2,3,4]);
Object.defineProperty(shadowed,'length',{get(){reads++;return 3;},configurable:true});
console.log('own length',lengthSum(shadowed),reads,prefix(shadowed));
const custom=new Uint8Array([1,2,3,4]);
Object.setPrototypeOf(custom,{get length(){reads++;return 3;}});
reads=0;console.log('prototype length',lengthSum(custom),reads);
const events:string[]=[];
const proxy=new Proxy(new Uint8Array([1,2,3]),{get(target,key){events.push(String(key));return Reflect.get(target,key,target);}});
console.log('proxy',lengthSum(proxy),events.join(','));
const inheritedProxy=new Uint8Array([1,2,3]);
Object.setPrototypeOf(inheritedProxy,new Proxy(Object.getPrototypeOf(inheritedProxy),{
 get(target,key,receiver){events.push('get:'+String(key));return Reflect.get(target,key,receiver);},
 getOwnPropertyDescriptor(target,key){events.push('describe:'+String(key));return Reflect.getOwnPropertyDescriptor(target,key);},
 getPrototypeOf(target){events.push('prototype');return Reflect.getPrototypeOf(target);}
}));
events.length=0;console.log('proxy prototype length',lengthSum(inheritedProxy),events.join(','));
events.length=0;console.log('proxy prototype fixed',prefix(inheritedProxy),events.join(','));
function writes(values:Uint8Array):number {let sum=0;for(let i=0;i<3;i++){sum+=values[i];values[i]=9;}return sum;}
console.log('writes',writes(new Uint8Array([1,2,3])));
function calls(values:Uint8Array,step:()=>void):number {let sum=0;for(let i=0;i<3;i++){sum+=values[i];step();}return sum;}
const changed=new Uint8Array([1,2,3]);console.log('calls',calls(changed,()=>{changed[1]=10;}));
const resizable=new ArrayBuffer(3,{maxByteLength:8});const tracked=new Uint8Array(resizable);tracked.set([1,2,3]);
console.log('resizable',lengthSum(tracked),prefix(tracked));
resizable.resize(2);console.log('resized',lengthSum(tracked),prefix(tracked));
const detachedBuffer=new ArrayBuffer(3),detached=new Uint8Array(detachedBuffer);detachedBuffer.transfer();
console.log('detached',lengthSum(detached),prefix(detached));
function empty():number {let sum=0;for(let i=0;i<0;i++)sum+=late[i];return sum;}
function skipped():number {let sum=0;for(let i=0;i<3;i++)if(false)sum+=late[i];return sum;}
console.log('uninitialized skipped',empty(),skipped());
const late=new Uint8Array([1,2,3]);
function captured():number {
 const run=()=>{let sum=0;for(let i=0;i<3;i++)if(false)sum+=local[i];return sum;};
 const result=run();const local=new Uint8Array([1,2,3]);return result+local[0];
}
console.log('captured skipped',captured());
function first(values:any):number {let value=0;for(let i=0;i<1;i++)value=values[i] as number;return value;}
const Half=(globalThis as any).Float16Array;
const formats:any[]=[
 new Int8Array([-128,-1,127,0]),new Uint8Array([0,255,128,1]),new Uint8ClampedArray([0,255,128,1]),
 new Int16Array([-32768,-1,32767,2]),new Uint16Array([0,65535,32768,1]),
 new Int32Array([-2147483648,-1,2147483647,2]),new Uint32Array([0,4294967295,2147483648,1]),
 new Half([1.25,-2.5,3.75,0.1]),new Float32Array([1.25,-2.5,3.75,0.1]),new Float64Array([1.25,-2.5,3.75,0.1])
];
for(const values of formats) {
 console.log('format',values.constructor.name,prefix(values),lengthSum(values),fromOne(values));
 console.log('format views',prefix(values.subarray(1,4)),prefix(values.subarray(0,3)));
}
for(const C of [Half,Float32Array,Float64Array]) {
 console.log('float special',Object.is(first(new C([-0])),-0),Number.isNaN(first(new C([NaN]))),first(new C([Infinity])),first(new C([-Infinity])));
}
