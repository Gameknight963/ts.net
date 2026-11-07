/// <reference lib="esnext.iterator" />
console.log("array",Iterator.from([1,2,3]).toArray().join("|"));
console.log("reduce",Iterator.from([1,2,3]).reduce((a,b,index)=>a+b+index),Iterator.from([1,2,3]).reduce((a,b)=>a+b,10));
console.log("predicates",Iterator.from([1,2,3]).some(x=>x===2),Iterator.from([1,2,3]).every(x=>x>0),Iterator.from([1,2,3]).find(x=>x>1));
console.log("empty",Iterator.from([]).some(()=>true),Iterator.from([]).every(()=>false),Iterator.from([]).find(()=>true),Iterator.from([]).reduce((a,b)=>a,undefined));
console.log("each",Iterator.from([4,5]).forEach((value,index)=>console.log("callback",value,index)));
try{Iterator.from([]).reduce((a,b)=>a);}catch(error){console.log("reduce error",(error as Error).name);}
function source(){let index=0;return Iterator.from<number>({next(){console.log("next",index);return {done:index>=3,value:index++};},return(){console.log("return");return {done:true as const,value:undefined};}});}
console.log("early",source().some(x=>x===1));console.log("early every",source().every(x=>x<1));console.log("early find",source().find(x=>x===0));
try{source().forEach(()=>{throw "callback failure";});}catch(error){console.log("throw",error);}
const generic:any=(Iterator.prototype as any).toArray;let position=0;console.log("generic",generic.call({next(){return {done:position>=2,value:position++};}}).join("|"));
for(const name of ["toArray","forEach","reduce","some","every","find"]){const method:any=(Iterator.prototype as any)[name];console.log("metadata",method.name,method.length);}
