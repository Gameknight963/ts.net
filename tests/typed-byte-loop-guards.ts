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
