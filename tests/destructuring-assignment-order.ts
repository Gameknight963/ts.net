export {};
function local():number{let x=0;[x]=[5];({x}={x:6});return x;}console.log("local",local());
let events="";const holder:any={};const key={toString(){events+="K";return "x";}};
function object():any{events+="T";return holder;}
function* input():Generator<number>{try{events+="N";yield 7;events+="N";yield 8;}finally{events+="C";}}
[object()[key as any]]=input();console.log("coerce order",events,holder.x);
const fixed=Object.create(null,{x:{value:1,writable:false}});events="";
try{[fixed.x]=input();}catch(error){console.log("readonly",(error as Error).name,events);}
let x:any;let closes=0;
const throwing={*[Symbol.iterator](){try{yield undefined;}finally{closes++;}}};
try{[x=(()=>{throw new Error("default");})()]=throwing;}catch(error){console.log("cleanup",(error as Error).message,closes);}
const badReturn={ [Symbol.iterator](){return {next(){return {value:undefined,done:false};},return():IteratorResult<any>{throw new Error("close");}};}};
try{[x=(()=>{throw new Error("primary");})()]=badReturn;}catch(error){console.log("precedence",(error as Error).message);}
class Base{set value(v:number){console.log("super",v);}}
class Child extends Base{#x=0;run():number{[this.#x]=[9];[super.value]=[10];return this.#x;}}
console.log("private",new Child().run());
function* gen():Generator<any>{let value:any;[value=yield "default"]=[];return value;}
const iterator=gen();console.log("yield",iterator.next().value,iterator.next(11).value);
async function main():Promise<void>{
 let value:any;const source:any[]=[];console.log("await identity",([value=await Promise.resolve(12)]=source)===source,value);
 events="";[object()[key as any]=await Promise.resolve(13)]=[undefined];console.log("await target",events,holder.x);
 let target:any={};({[key as any]:target.x=await Promise.resolve(14)}={});console.log("await object",target.x);
 function* fail():Generator<any>{try{yield undefined;}finally{events+="C";}}
 events="";try{[value=await Promise.reject(new Error("rejected"))]=fail();}catch(error){console.log("reject",(error as Error).message,events);}
 try{[value=await Promise.reject(new Error("primary"))]=badReturn;}catch(error){console.log("await precedence",(error as Error).message);}
}
main().catch(error=>console.log("failure",String(error)));

let numbers:number[]=[];[...numbers]=[1,2,3];console.log("numbers",numbers.join(","));
const typedHolder:{values:number[]}={values:[]};[...typedHolder.values]=[4,5];console.log("property",typedHolder.values.join(","));
let flag=false;[flag]=[true];console.log("boolean",flag);
let text="";[text="fallback"]=[];console.log("string",text);
