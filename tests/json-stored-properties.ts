export {};
const getter:any=JSON.parse('{"a":1,"b":2,"c":3}');const log:string[]=[];
getter.a={toJSON(){Object.defineProperty(getter,"b",{get(){log.push("getter");return 9;},enumerable:true,configurable:true});return 7;}};
console.log("new getter",JSON.stringify(getter),log.join(","));
const removed:any=JSON.parse('{"a":1,"b":2}');let inheritedReads=0;
Object.setPrototypeOf(removed,{get b(){inheritedReads++;return this===removed?11:-1;}});
removed.a={toJSON(){delete removed.b;return 3;}};
console.log("deleted",JSON.stringify(removed),inheritedReads);
const values:any=JSON.parse('{"a":1,"b":2,"c":3,"d":4}');
const fn:any=function(){};fn.toJSON=()=>13;
values.a=fn;values.b=undefined;values.c=Symbol("value");values.d=new Number(17);
console.log("kinds",JSON.stringify(values));
const rewritten:any=JSON.parse('{"a":1,"b":2}');
rewritten.a={toJSON(){rewritten.b={toJSON(){return 19;}};return 5;}};
console.log("new hook",JSON.stringify(rewritten));
const replaced:any=JSON.parse('{"a":1,"b":2}');
console.log("replacer",JSON.stringify(replaced,(key,value)=>{if(key==="a")replaced.b=23;return value;}));
const hidden:any=JSON.parse('{"a":1,"b":2}');Object.defineProperty(hidden,"b",{value:29,enumerable:false});
console.log("hidden",JSON.stringify(hidden),JSON.stringify(hidden,["b","a"]));
console.log("pretty",JSON.stringify(JSON.parse('{"a":1,"b":null}'),null,2));
const cycle:any=JSON.parse('{"a":1}');cycle.a=cycle;
try{JSON.stringify(cycle);}catch{console.log("cycle");}
const bigint:any=JSON.parse('{"a":1}');bigint.a=3n;
try{JSON.stringify(bigint);}catch{console.log("bigint");}

const scalar:any=JSON.parse('{"2":2,"1":1,"text":"item","flag":true,"empty":null}');
scalar.skipped=undefined;scalar.negativeZero=-0;scalar.nan=NaN;scalar.infinity=Infinity;
console.log("scalars",JSON.stringify(scalar));
const counter:any=JSON.parse('{"count":0,"name":"item"}');
for(let i=0;i<10;i++)counter.count+=1;
console.log("numeric updates",JSON.stringify(counter));
for(const count of [4,5,16,17]){const value:any=JSON.parse('{}');for(let i=0;i<count;i++)value["field"+i]=i;console.log("size",count,JSON.stringify(value));}
