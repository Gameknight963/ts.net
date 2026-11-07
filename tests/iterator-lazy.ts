/// <reference lib="esnext.iterator" />
function source(){let index=0;return Iterator.from<number>({next(){console.log("next",index);return {done:index>=5,value:index++};},return(){console.log("return");return {done:true as const,value:undefined};}});}
const lazy=source().map((value,index)=>{console.log("map",value,index);return value*2;}).filter((value,index)=>{console.log("filter",value,index);return value>1;}).take(2);console.log("created");console.log(lazy.toArray().join("|"));
console.log("drop",source().drop(2).take(1).toArray().join("|"));console.log("zero",source().take(0).toArray().length);
console.log("fraction",Iterator.from([1,2,3]).take(1.9).toArray().join("|"));console.log("infinity",Iterator.from([1,2]).drop(Infinity).toArray().length);
const unused:any=source().map(x=>x);console.log("return before",JSON.stringify(unused.return()),JSON.stringify(unused.next()));
const failure=source().map(()=>{throw "callback";});try{failure.next();}catch(error){console.log("failure",error);}console.log("after",JSON.stringify(failure.next()));
for(const name of ["take","drop"])for(const value of [NaN,-1,-Infinity])try{(Iterator.from([]) as any)[name](value);}catch(error){console.log("limit",(error as Error).name);}
for(const name of ["map","filter"])try{(Iterator.from([]) as any)[name](42);}catch(error){console.log("callback error",(error as Error).name);}
let recursive:any;recursive=Iterator.from([1]).map(()=>recursive.next());try{recursive.next();}catch(error){console.log("recursive",(error as Error).name);}
for(const name of ["map","filter","take","drop"]){const method:any=(Iterator.prototype as any)[name];console.log("metadata",method.name,method.length);}
