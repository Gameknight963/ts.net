export {};
class Derived extends Uint8Array{}
class Wide extends Uint8Array{static get [Symbol.species]():any{return Uint16Array;}}
class Ordinary extends Uint8Array{static get [Symbol.species]():any{return null;}}
for(const C of [Derived,Wide,Ordinary])for(const method of ["map","filter","slice","subarray"]){const source:any=new C([1,2,3,4]);let result:any;if(method==="map")result=source.map((x:number)=>x+255);else if(method==="filter")result=source.filter((x:number)=>x>2);else if(method==="slice")result=source.slice(1,3);else result=source.subarray(0,2);console.log(C.name,method,result.constructor.name,result instanceof Derived,result.length,Array.from(result).join("|"),result.buffer===source.buffer);}
console.log("getter",(Uint8Array as any)[Symbol.species]===Uint8Array,(Derived as any)[Symbol.species]===Derived);
for(const species of [Array,()=>{},1,class{constructor(){return {}; }},class{constructor(){return new Uint8Array(0);}}])for(const method of ["map","filter","slice","subarray"]){const source:any=new Uint8Array([1,2]);source.constructor={[Symbol.species]:species};let calls=0;try{const result=source[method](...((method==="map"||method==="filter")?[(x:number)=>{calls++;return x;}]:[]));console.log("result",method,result.length,calls);}catch(error){console.log("invalid",method,(error as Error).name,calls);}}
class Big extends Uint8Array{static get [Symbol.species]():any{return BigUint64Array;}}
const big:any=new Big([1,2]);console.log("big map",Array.from(big.map((x:number)=>BigInt(x))).map(String).join("|"));for(const method of ["filter","slice"])try{big[method](...((method==="filter")?[()=>true]:[]));}catch(error){console.log("content",method,(error as Error).name);}
const ignored:any=new Derived([3,1,2]);ignored.constructor={get [Symbol.species](){throw "must not read";}};console.log("copy",ignored.toReversed().constructor.name,ignored.toSorted().join("|"),ignored.with(0,8).join("|"));

let events="";const ordered:any=new Uint8Array([1,2]);ordered.constructor={get [Symbol.species](){events+="S";return class{constructor(n:number){events+="C"+n;return new Uint8Array(n);}};}};
ordered.map((x:number)=>{events+="M";return x;});console.log("map order",events);events="";ordered.filter((x:number)=>{events+="F";return x>1;});console.log("filter order",events);
let shared=new ArrayBuffer(8);class Aliased extends Uint8Array{static get [Symbol.species]():any{return class{constructor(n:number){return new Uint8Array(shared,1,n);}};}}
const aliased:any=new Aliased(shared,0,6);aliased.set([1,2,3,4,5,6]);console.log("overlap bytes",Array.from(aliased.slice(0,4)).join("|"),Array.from(new Uint8Array(shared)).join("|"));
shared=new ArrayBuffer(8);class AliasedWide extends Uint8Array{static get [Symbol.species]():any{return class{constructor(n:number){return new Uint16Array(shared,0,n);}};}}
const wide:any=new AliasedWide(shared,0,4);wide.set([1,2,3,4]);console.log("overlap conversion",Array.from(wide.slice()).join("|"));
class Numbers extends BigInt64Array{static get [Symbol.species]():any{return Float64Array;}}
const numbers:any=new Numbers([1n,2n]);console.log("number map",Array.from(numbers.map((x:bigint)=>Number(x))).join("|"));
class Longer extends Uint8Array{static get [Symbol.species]():any{return class{constructor(n:number){return new Uint8Array(n+2).fill(9);}};}}
const longer:any=new Longer([1,2]);console.log("longer",longer.map((x:number)=>x).join("|"),longer.filter((x:number)=>x>1).join("|"),longer.slice().join("|"));
for(const constructor of [undefined,null,1,{[Symbol.species]:undefined},{[Symbol.species]:null}]){const a:any=new Uint8Array([1,2]);a.constructor=constructor;try{console.log("constructor",a.slice().join("|"));}catch(error){console.log("constructor error",(error as Error).name);}}
const empty:any=new Big(0);console.log("empty",empty.slice().constructor.name,empty.filter(()=>true).constructor.name,empty.map(()=>0n).constructor.name);

const bits=new ArrayBuffer(8);new DataView(bits).setBigUint64(0,0x7ff0000000000001n,true);const floats=new Float64Array(bits);const floatCopy=floats.slice();console.log("nan bits",new DataView(floatCopy.buffer).getBigUint64(0,true).toString(16));
