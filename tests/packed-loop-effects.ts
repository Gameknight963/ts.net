export {};
const numeric:number[]=[1,2,3];
let numericGets=0;
const source={get value():number{numericGets++;numeric.length=0;return 11;}};
for(let index=0;index<2;index++)numeric[index]=source.value+0;
console.log("getter",numeric.length,numeric.join(","),numericGets);
const mapped=[1,2,3].map(value=>value);
let mappedGets=0;
const mappedSource={get value():number{mappedGets++;mapped.length=0;return 13;}};
for(let index=0;index<2;index++)mapped[index]=mappedSource.value+0;
console.log("mapped getter",mapped.length,mapped.join(","),mappedGets);
const constructed:number[]=[1,2,3];
class Reset {constructor(){constructed.length=0;}}
for(let index=0;index<2;index++){new Reset();constructed[index]=index+17;}
console.log("constructor",constructed.length,constructed.join(","));
const deleted=[1,2,3].map(value=>value);
for(let index=0;index<2;index++){delete deleted[index];deleted[index]=index+19;}
console.log("delete",deleted.join(","),Object.keys(deleted).join(","));
const spread:number[]=[1,2,3];
const spreadSource={get value():number{spread.length=0;return 1;}};
for(let index=0;index<2;index++){const copy={...spreadSource};spread[index]=index+23;}
console.log("spread",spread.length,spread.join(","));
const iterated:number[]=[1,2,3];
const iterable:Iterable<number>={[Symbol.iterator](){return {next(){iterated.length=0;return {done:true,value:0};}};}};
for(let index=0;index<2;index++){const copy=[...iterable];iterated[index]=index+29;}
console.log("iterator",iterated.length,iterated.join(","));
