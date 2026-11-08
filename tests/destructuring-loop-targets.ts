export {};
let key="",value=0;
for([key,value] of [["a",1],["b",2]] as [string,number][])console.log("pair",key,value);
console.log("existing",key,value);
let rest:number[]=[];
for([value=7,...rest] of [[undefined,1,2],[3,4]] as [number|undefined,...number[]][])console.log("rest",value,rest.join(","));
const holder:any={};
for({x:holder.value=8} of [{x:undefined},{x:9}])console.log("object",holder.value);
let field:any,other:any;
for({x:field,...other} of [{x:1,y:2},{x:3,z:4}])console.log("object rest",field,JSON.stringify(other));
const callbacks:(()=>number)[]=[];for([value] of [[10],[11]])callbacks.push(()=>value);
console.log("shared binding",callbacks.map(callback=>callback()).join(","));
let named:any;for([named=()=>0] of [[]])console.log("name",named.name);
let events="";
function* outer():Generator<any>{try{yield { [Symbol.iterator]:function*(){try{yield undefined;}finally{events+="I";}}};}finally{events+="O";}}
try{for([field=(()=>{throw new Error("default");})()] of outer())console.log("unexpected");}catch(error){console.log("cleanup",(error as Error).message,events);}
events="";for([field] of outer()){break;}console.log("break",events);
function* generate():Generator<any>{let item:any;for([item=yield "default"] of [[]])yield item;return item;}
const iterator=generate();console.log("generator",iterator.next().value,iterator.next(12).value,iterator.next().value);
async function main():Promise<void>{
 let item:any;
 for await([item=await Promise.resolve(13)] of [[],[14]])console.log("async",item);
 let target:any={};for await({x:target.value=await Promise.resolve(15)} of [{}, {x:16}])console.log("async object",target.value);
 events="";try{for await([item=await Promise.reject(new Error("rejected"))] of outer())console.log("unexpected");}catch(error){console.log("async cleanup",(error as Error).message,events);}
 async function* pairs():AsyncGenerator<number[]>{try{yield [17];yield [18];}finally{events+="A";}}
 events="";for await([item] of pairs()){console.log("async source",item);break;}console.log("async source close",events);
}
main().catch(error=>console.log("failure",String(error)));
