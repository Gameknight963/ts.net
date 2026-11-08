/// <reference lib="esnext.iterator" />
export {};
let events="";
const iterator:any={ [Symbol.iterator](){events+="I";return {get next(){events+="N";return ()=>{events+="S";return {done:true};};},return(){events+="C";return {};}};}};
const []=iterator;console.log("declaration",events);
events="";console.log("identity",([]=iterator)===iterator,events);
for(const value of [null,undefined,1,"abc",{},[]]){try{const {}=value as any;console.log("object",String(value));}catch(error){console.log("object error",(error as Error).name);}try{const []=value as any;console.log("array",String(value));}catch(error){console.log("array error",(error as Error).name);}}
let x:any;[{x}={} as any]=([undefined] as any[]);console.log("nested",x);
async function main():Promise<void>{let value:any;[[] = await Promise.resolve(iterator)]=([undefined] as any[]);console.log("await empty",events);}
main().catch(error=>console.log("failure",String(error)));

export {};
let effects="";
const source:any={[Symbol.iterator](){effects+="I";return {get next(){effects+="N";return 1;},return(){effects+="C";return {};}};}};
try{const []=source;console.log("empty",effects);}catch(error){console.log("empty error",(error as Error).name,effects);}
effects="";try{let value:any;[value]=source;}catch(error){console.log("advance",(error as Error).name,effects);}
effects="";const direct:any={get next(){effects+="N";return 1;},return(){effects+="C";return {};}};
try{const helper=Iterator.prototype.take.call(direct,0);console.log("zero take",helper.next().done,effects);}catch(error){console.log("zero error",(error as Error).name,effects);}

for(const next of [undefined,null,1,{}]){
 let reads=0,closed=0;
 const iterable:any={[Symbol.iterator](){return {get next(){reads++;return next;},return(){closed++;return {};}};}};
 const []=iterable;console.log("unused next",reads,closed);
}
for(const iterator of [[].values(),new Map().entries(),"a".matchAll(/a/g)]){
 const native:any=iterator;native.next=1;let closed=0;native.return=()=>{closed++;return {};};
 const []=native;console.log("native",closed);
}
let returns=0;
const invalidGetter:any={[Symbol.iterator](){return {get next(){throw new Error("get next");},return(){returns++;return {};}};}};
try{const []=invalidGetter;}catch(error){console.log("getter failure",(error as Error).message,returns);}
