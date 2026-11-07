export {};
class Derived extends Array<number>{}
class Other extends Array<number>{}
class Custom extends Array<number>{static get [Symbol.species]():any{return Other;}}
class Ordinary extends Array<number>{static get [Symbol.species]():ArrayConstructor{return Array;}}
for(const C of [Derived,Custom,Ordinary])for(const method of ["map","filter","slice","concat","splice","flat","flatMap"]){const source=new C(1,2,3);let result:any;if(method==="map")result=source.map(x=>x*2);else if(method==="filter")result=source.filter(x=>x>1);else if(method==="slice")result=source.slice(1);else if(method==="concat")result=source.concat([4]);else if(method==="splice")result=source.splice(1,1);else if(method==="flat")result=source.flat();else result=source.flatMap(x=>[x,x]);console.log(C.name,method,result instanceof Derived,result instanceof Other,result.constructor===Array,result.join("|"));}
console.log("getter",Array[Symbol.species]===Array,Derived[Symbol.species]===Derived);
class Result{length:number;constructor(length:number){this.length=length;}}
for(const method of ["map","filter","slice","concat","splice","flat","flatMap"]){const source:any=[1,,3];source.constructor={[Symbol.species]:Result};let result:any;if(method==="map")result=source.map((x:number)=>x*2);else if(method==="filter")result=source.filter(()=>true);else if(method==="slice")result=source.slice();else if(method==="concat")result=source.concat([4]);else if(method==="splice")result=source.splice(0,2);else if(method==="flat")result=source.flat();else result=source.flatMap((x:number)=>[x]);console.log("object",method,result instanceof Result,result.length,Object.keys(result).join("|"),result[0],result[1],result[2]);}

for(const constructor of [undefined,null,1,"text",{[Symbol.species]:null},{[Symbol.species]:undefined},{[Symbol.species]:1},{[Symbol.species]:()=>{}}]){const array:any=[1,2];array.constructor=constructor;try{console.log("constructor",array.map((x:number)=>x+1).join("|"));}catch(error){console.log("constructor error",(error as Error).name);}}
let effects="";const source:any=[1,2,3];source.constructor={get [Symbol.species](){effects+="S";return class extends Array{constructor(length:number){super(length);effects+="C"+length;}};}};
console.log("order",source.map((x:number)=>{effects+="M";return x;}).join("|"),effects);
effects="";try{source.map(null);}catch(error){console.log("callback first",(error as Error).name,effects);}
const changes:any=[1,2,3];changes.constructor={get [Symbol.species](){changes.length=1;return Array;}};console.log("changed slice",JSON.stringify(changes.slice()),Object.keys(changes.slice()).join("|"));
const ignored:any=[3,1,2];ignored.constructor={get [Symbol.species](){throw "must not read";}};console.log("copy methods",ignored.toReversed().join("|"),ignored.toSorted().join("|"),ignored.toSpliced(1,1).join("|"),ignored.with(0,8).join("|"));
let getterCalls=0;const once:any=[1];Object.setPrototypeOf(once,{get constructor(){getterCalls++;return {[Symbol.species]:null};}});console.log("once",Array.prototype.map.call(once,(x:any)=>x).length,getterCalls);

const proto:any=Array.prototype;proto[1]=8;try{const copied=[1,,3].slice();console.log("prototype holes",copied.join("|"),Object.keys(copied).join("|"));}finally{delete proto[1];}

const sparse:number[]=[];sparse[999999]=3;const sparseCopy=sparse.slice();console.log("sparse",sparseCopy.length,Object.keys(sparseCopy).join("|"),sparseCopy[999999]);

const descriptorSource:any=[2];descriptorSource.constructor={[Symbol.species]:class{constructor(){return Object.create(null,{0:{get(){throw "getter must not run";},configurable:true}});}}};
const descriptorResult=descriptorSource.map((x:number)=>x*3);const d=Object.getOwnPropertyDescriptor(descriptorResult,"0")!;console.log("define item",descriptorResult[0],d.value,d.enumerable,d.configurable,d.writable);
const locked:any=[2];locked.constructor={[Symbol.species]:class{constructor(){return Object.create(null,{0:{value:0}});}}};try{locked.map((x:number)=>x);}catch(error){console.log("locked",(error as Error).name);}
const generic:any={0:4,length:1,get constructor(){throw "must not read";}};console.log("generic",Array.prototype.map.call(generic,(x:any)=>x).join("|"));
const meta=Object.getOwnPropertyDescriptor(Array,Symbol.species)!;console.log("metadata",meta.enumerable,meta.configurable,meta.get!.name,meta.get!.length,meta.set);

const literalSource:any=[2];literalSource.constructor={[Symbol.species]:class{constructor(length:number){return {0:99,length} as any;}}};const literalResult=literalSource.map((x:number)=>x*3);console.log("literal result",literalResult[0],literalResult.length);
let setters=0;const setterSource:any=[2];setterSource.constructor={[Symbol.species]:class{constructor(){return Object.create({set 0(value:number){setters++;}});}}};const setterResult=setterSource.map((x:number)=>x*3);console.log("own result",setterResult[0],setters,Object.hasOwn(setterResult,"0"));
