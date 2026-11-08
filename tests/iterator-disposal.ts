/// <reference lib="esnext.iterator" />
/// <reference lib="esnext.disposable" />
export {};
let events="";
function* source():Generator<number>{try{yield 1;yield 2;}finally{events+="C";}}
{using iterator=source();console.log("using",iterator.next().value,events);}console.log("closed",events);
events="";{using iterator=source().map(value=>value*2);console.log("helper",iterator.next().value);}console.log("helper closed",events);
const dispose:any=Iterator.prototype[Symbol.dispose];
console.log("metadata",dispose.name,dispose.length,Object.getOwnPropertyDescriptor(Iterator.prototype,Symbol.dispose)!.enumerable);
const target={return(...args:any[]){console.log("return",this===target,args.length);return 123;}};
console.log("generic",dispose.call(target),dispose.call({}),dispose.call({return:null}),dispose.call(1));
for(const value of [null,undefined,{return:1}])try{dispose.call(value);}catch(error){console.log("invalid",(error as Error).name);}
let reads=0;console.log("getter",dispose.call({get return(){reads++;return ()=>undefined;}}),reads);
const token={message:"failure"};try{dispose.call({return(){throw token;}});}catch(error){console.log("identity",error===token);}
const array=[1,2].values();array[Symbol.dispose]();console.log("array noop",array.next().value);
async function* asyncSource():AsyncGenerator<number>{try{yield 3;yield 4;}finally{events+="A";await Promise.resolve();events+="B";}}
async function main():Promise<void>{
 events="";{await using iterator=asyncSource();console.log("await using",(await iterator.next()).value);}console.log("async closed",events);
 const temporary=asyncSource();const asyncDispose:any=temporary[Symbol.asyncDispose];
 console.log("async metadata",asyncDispose.name,asyncDispose.length);
 const receiver={return(...args:any[]){console.log("async return",this===receiver,args.length,args[0]);return 123;}};
 const pending=asyncDispose.call(receiver);console.log("promise",pending instanceof Promise);console.log("async generic",await pending);
 console.log("async absent",await asyncDispose.call({}));
 for(const value of [null,undefined,{return:1}]){const promise=asyncDispose.call(value);console.log("rejected promise",promise instanceof Promise);try{await promise;}catch(error){console.log("async invalid",(error as Error).name);}}
 try{await asyncDispose.call({get return(){throw token;}});}catch(error){console.log("async getter identity",error===token);}
 try{await asyncDispose.call({return(){return Promise.reject(token);}});}catch(error){console.log("async identity",error===token);}
 let settled=false;await asyncDispose.call({return(){return {then(resolve:(value:any)=>void){settled=true;resolve("ignored");}};}});console.log("thenable",settled);
 const shared=new Blob(["x"]).stream().values();console.log("shared async method",shared[Symbol.asyncDispose]===asyncDispose);await shared[Symbol.asyncDispose]();
 {await using iterator=new Blob(["abc"]).stream().values();console.log("stream",(await iterator.next()).value!.length);}
 console.log("stream disposed");
}
main().catch(error=>console.log("failure",String(error)));
