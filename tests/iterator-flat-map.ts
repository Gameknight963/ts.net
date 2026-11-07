/// <reference lib="esnext.iterator" />
const lazy=Iterator.from([1,2]).flatMap((value,index)=>{console.log("mapper",value,index);return [value,value+10];});console.log("created");console.log(lazy.toArray().join("|"));
console.log("skip",Iterator.from([1,2,3]).flatMap(x=>x===2?[]:[x]).toArray().join("|"));console.log("iterators",Iterator.from([1,2]).flatMap(x=>[x,x+1].values()).toArray().join("|"));
function source(name:string){let index=0;return Iterator.from<number>({next(){console.log(name,"next",index);return {done:index>=2,value:index++};},return(){console.log(name,"return");return {done:true as const,value:undefined};}});}
console.log("cleanup",source("outer").flatMap(()=>source("inner")).take(1).toArray().join("|"));
const unused:any=source("unused").flatMap(()=>source("unopened"));console.log("before start",JSON.stringify(unused.return()));
try{source("bad").flatMap((()=>"abc") as any).toArray();}catch(error){console.log("primitive",(error as Error).name);}
try{source("throw").flatMap(()=>{throw "mapper failure";}).toArray();}catch(error){console.log("callback",error);}
try{source("failing outer").flatMap(()=>({next(){throw "inner failure";},return(){console.log("unexpected return");return {done:true as const,value:undefined};}})).toArray();}catch(error){console.log("inner",error);}
console.log("metadata",Iterator.prototype.flatMap.name,Iterator.prototype.flatMap.length);
