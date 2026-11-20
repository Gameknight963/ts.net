export {};
function sum(a:any,b:any,c:any):any{return a+b+c;}
function grouped(a:any,b:any,c:any):any{return a+(b+c);}
function fields(row:any):any{return row.a+row.b+row.c;}
function groupedFields(row:any):any{return row.a+(row.b+row.c);}
function checked(a:any,b:any,c:any):number{return a+b+c;}
function numericFields(row:any):number{return row.a+row.b+row.c;}
function numericGrouped(row:any):number{return row.a+(row.b+row.c);}
function accumulate(row:any):number {let total=7;total+=row.a+row.b+row.c;return total;}
for(const a of [undefined,null,false,true,-0,0,1000.5,NaN,Infinity,'','text'])
 for(const b of [undefined,null,false,true,-0,2048.5,'tail'])
  for(const c of [undefined,null,1,'end']) console.log(String(sum(a,b,c)),String(grouped(a,b,c)));
console.log('bigint',sum(1n,2n,3n),grouped(1n,2n,3n));
console.log('zeros',Object.is(sum(-0,-0,-0),-0),Object.is(checked(-0,-0,-0),-0));
console.log('checked',checked(1000,2000,3000),checked(null,true,3),checked(undefined,2,3));
const parsed=JSON.parse('{"a":1000.5,"b":2000.5,"c":3000.5}');
console.log('parsed',fields(parsed),groupedFields(parsed),accumulate(parsed));
parsed.b=undefined;console.log('undefined',fields(parsed),accumulate(parsed));
parsed.a='head';console.log('text',fields(parsed));
let trace:string[]=[];
const row:any={
 get a(){trace.push('get a');return {valueOf(){trace.push('value a');return 4;}};},
 get b(){trace.push('get b');return {valueOf(){trace.push('value b');return 5;}};},
 get c(){trace.push('get c');return {valueOf(){trace.push('value c');return 6;}};}
};
console.log('order',fields(row),trace.join(','));trace=[];
console.log('grouped order',groupedFields(row),trace.join(','));trace=[];
const changing:any={a:{valueOf(){trace.push('convert');changing.c=20;return 3;}},b:4,c:5};
console.log('mutation',fields(changing),trace.join(','));
trace=[];const primitive:any={a:{[Symbol.toPrimitive](hint:string){trace.push(hint);return 'x';}},b:undefined,c:null};
console.log('primitive',fields(primitive),trace.join(','));
let reads=0;const proxy=new Proxy(parsed,{get(target,key,receiver){reads++;return Reflect.get(target,key,receiver);}});
console.log('proxy',fields(proxy),reads);
const frozen=Object.freeze(JSON.parse('{"a":10,"b":20,"c":30}'));
console.log('frozen',fields(frozen),accumulate(frozen));
const inherited:any=JSON.parse('{"a":1,"b":2,"c":3}');delete inherited.b;
Object.setPrototypeOf(inherited,{get b(){reads++;return 7;}});reads=0;console.log('inherited',fields(inherited),reads);
const nullish:any={a:1,b:null,c:2};console.log('optional',nullish.a+nullish.b?.x+nullish.c);
trace=[];const thrown:any={get a(){trace.push('a');return Symbol();},get b(){trace.push('b');return 2;},get c(){trace.push('c');return 3;}};
try{fields(thrown);}catch{console.log('symbol error',trace.join(','));}
trace=[];const bad:any={get a(){trace.push('a');return {valueOf(){trace.push('coerce');return 1;}};},get b(){trace.push('b');throw Error('stop');},get c(){trace.push('c');return 3;}};
try{fields(bad);}catch{console.log('getter error',trace.join(','));}
async function asyncSum(a:any,b:any,c:any):Promise<any>{return (await Promise.resolve(a))+b+c;}
asyncSum(1000,2000,3000).then(value=>console.log('async',value));
function* generator():Generator<any,any,any>{return (yield 1)+(yield 2)+3;}
const it=generator();console.log('generator',it.next().value,it.next(10).value,it.next(20).value);
function long(a:any):any{return a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a;}
console.log('long',long(1024));

for(const a of [undefined,null,false,true,-0,0,1000.5,NaN,Infinity])
 for(const b of [undefined,null,false,true,-0,2048.5])
  console.log('numeric matrix',checked(a,b,3),checked(3,a,b));
trace=[];console.log('numeric order',numericFields(row),trace.join(','));
trace=[];console.log('numeric grouped order',numericGrouped(row),trace.join(','));
trace=[];changing.c=5;console.log('numeric mutation',numericFields(changing),trace.join(','));
console.log('numeric frozen',numericFields(frozen),numericGrouped(frozen));
reads=0;console.log('numeric inherited',numericFields(inherited),reads);
const numericProxy=new Proxy(frozen,{get(target,key,receiver){reads++;return Reflect.get(target,key,receiver);}});
reads=0;console.log('numeric proxy',numericFields(numericProxy),reads);
trace=[];try{numericFields(thrown);}catch{console.log('numeric symbol error',trace.join(','));}
trace=[];try{numericFields(bad);}catch{console.log('numeric getter error',trace.join(','));}
async function checkedAsync(a:any,b:any,c:any):Promise<number>{return (await Promise.resolve(a))+b+c;}
checkedAsync(1000,2000,3000).then(value=>console.log('numeric async',value));

function* checkedGenerator():Generator<any,number,any>{return (yield 1)+(yield 2)+3;}
const checkedIt=checkedGenerator();console.log('numeric generator',checkedIt.next().value,checkedIt.next(10).value,checkedIt.next(20).value);
function longNumber(a:any):number{return a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a+a;}
console.log('long numeric',longNumber(1024));
