/// <reference lib="es2023.array" />
export {};
function show(label:string,array:any) {
 const text=(value:any)=>typeof value==='number' && Number.isNaN(value)?'NaN':Object.is(value,-0)?'-0':String(value);
 const before=Array.from(array,text).join(',');const sorted=array.toSorted();
 console.log(label,'copy',Array.from(sorted,text).join(','),Array.from(array,text).join(',')===before,sorted.buffer!==array.buffer);
 console.log(label,'sort',array.sort()===array,Array.from(array,text).join(','));
}
const constructors:any[]=[Int8Array,Uint8Array,Uint8ClampedArray,Int16Array,Uint16Array,Int32Array,Uint32Array];
for(const C of constructors)show(C.name,new C([4,1,3,2,1,0,-1,-5,255,65535]));
for(const C of [BigInt64Array,BigUint64Array])show(C.name,new C([4n,1n,3n,2n,1n,0n,-1n,-9223372036854775808n,9223372036854775807n]));
show('float32',new Float32Array([NaN,0,-0,Infinity,-Infinity,3,-1,NaN]));
show('float64',new Float64Array([NaN,0,-0,Infinity,-Infinity,3,-1,NaN]));
const Float16=(globalThis as any).Float16Array;if(typeof Float16==='function')show('float16',new Float16([NaN,0,-0,3,-1,NaN]));
const all=new Int32Array([99,4,1,3,2,88]);show('offset',all.subarray(1,5));console.log('sentinels',all[0],all[5]);
const empty=new Uint16Array(0);show('empty',empty);show('single',new Uint32Array([42]));
const buffer:any=new (ArrayBuffer as any)(24,{maxByteLength:48});const fixed=new Int32Array(buffer,4,3);fixed.set([4,1,3]);show('resizable fixed',fixed);
const tracking=new Int32Array(buffer,4);tracking.set([9,7,8,6,5]);buffer.resize(32);show('resizable tracking',tracking);
const shared=new Int32Array(new SharedArrayBuffer(16));shared.set([4,1,3,2]);show('shared',shared);
const bytes=new ArrayBuffer(32);const view=new DataView(bytes);view.setBigUint64(0,0x7ff8000000000001n,true);view.setFloat64(8,3,true);view.setBigUint64(16,0x7ff8000000000002n,true);view.setFloat64(24,1,true);
const floating=new Float64Array(bytes);floating.sort();console.log('nan payloads',view.getBigUint64(16,true).toString(16),view.getBigUint64(24,true).toString(16));

const traps=new Int32Array([4,1,3,2]);
Object.defineProperty(traps,'length',{get(){throw new Error('length');}});
Object.defineProperty(traps,'constructor',{get(){throw new Error('constructor');}});show('ignored properties',traps);
const nonExtensible=new Int32Array([4,1,3,2]);Object.preventExtensions(nonExtensible);show('nonextensible',nonExtensible);
const growable:any=new (SharedArrayBuffer as any)(16,{maxByteLength:32});const growing=new Uint16Array(growable,2);growing.set([4,1,3,2]);growable.grow(32);show('growable shared',growing);
