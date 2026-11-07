/// <reference lib="esnext.iterator" />
for(const source of [[1,2],"abc",new Set([3,4]),new Map([[1,"a"]])])console.log("from",JSON.stringify([...Iterator.from(source as any)]));
const native=[1,2].values();console.log("identity",Iterator.from(native)===native,native instanceof Iterator);
let position=0;const source={next(){return {value:position++,done:position>3};},return(){console.log("return");return {done:true as const,value:"closed"};}};
const wrapper=Iterator.from(source);console.log("wrapped",(wrapper as any)!==source,wrapper instanceof Iterator,wrapper.next().value);for(const value of wrapper){console.log("value",value);break;}
console.log("array",Array.from(Iterator.from({next(){return {done:true,value:0};}})).length);
const invalid=Iterator.from({next:1} as any);try{invalid.next();}catch(error){console.log("invalid next",(error as Error).name);}
for(const value of [null,undefined,1,true])try{Iterator.from(value as any);}catch(error){console.log("invalid",(error as Error).name);}
class Counter extends Iterator<number>{position=0;next():IteratorResult<number>{return {value:this.position++,done:this.position>3};}}console.log("subclass",[...new Counter()].join("|"));
try{new (Iterator as any)();}catch(error){console.log("abstract",(error as Error).name);}
console.log("metadata",Iterator.name,Iterator.length,Iterator.from.name,Iterator.from.length);
let gets=0;const captured={value:5,get next(){gets++;return function(this:{value:number}){return {done:false,value:this.value};};}};const cached=Iterator.from(captured);captured.value=6;console.log("capture",cached.next().value,gets);
console.log("unicode",[...Iterator.from(String.fromCodePoint(0x1f600))].length);
console.log("counter array",Array.from(new Counter()).join("|"));
(Array as any).fromAsync(new Counter()).then((values:number[])=>console.log("async counter",values.join("|")));
