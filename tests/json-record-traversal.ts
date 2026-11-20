export {};
function make(count:number):any[]{return JSON.parse(JSON.stringify(Array.from({length:count},(_,i)=>({a:i+1000.5,b:i+2000.5,c:i+3000.5,d:i+4000.5,e:i+5000.5}))));}
function sum(rows:any[]):number{let total=0;for(let i=0;i<rows.length;i++)total+=rows[i].a+rows[i].b+rows[i].e;return total;}
function literal(rows:any[]):number{let total=0;for(let i=0;i<64;i++)total+=rows[i].a+rows[i].b+rows[i].e;return total;}
function alias(rows:any[]):number{let total=0;for(let i=0;i<rows.length;i++){const row=rows[i];total+=row.a+row.b+row.e;}return total;}
function typed(rows:{a:number;b:number;e:number}[]):number{let total=0;for(let i=0;i<rows.length;i++)total+=rows[i].a+rows[i].b+rows[i].e;return total;}
function one(rows:{a:number;b:number|undefined}[]):number{let total=0;for(let i=0;i<rows.length;i++)total+=rows[i].a;return total;}
function arithmetic(rows:any[]):number{let total=0;for(let i=3;i<rows.length;i++)total+=-(rows[i].a-rows[i].b*2)+rows[i].e/2+i;return total;}
function asserted(rows:any[]):number{let total=0;for(let i=0;i<rows.length;i++)total+=(rows[i] as {a:number;b:number;e:number}).a+(rows[i].b as number)+rows[i].e;return total;}
const data=make(96);console.log('numeric',sum(data),literal(data),alias(data),typed(data),asserted(data),arithmetic(data));
console.log('small',sum(make(3)),sum([]));
const optional=make(96);optional[30].b=undefined;console.log('unused undefined',one(optional));
data[90].a=NaN;console.log('nan',sum(data),typed(data));data[90].a=1090.5;
data[40].e=undefined;console.log('undefined',sum(data),typed(data));data[40].e=5040.5;
const references=make(96);references[30].e=true;references[60].e=null;console.log('reference fallback',sum(references));
const unusedReferences=JSON.parse(JSON.stringify(Array.from({length:96},()=>({a:1,b:2,c:'unused',d:true,e:5}))));console.log('unused references',sum(unusedReferences),typed(unusedReferences));
let gets=0;const changing=make(96);
Object.defineProperty(changing[40],'e',{get(){gets++;if(gets===1)changing.push({a:9001,b:9002,c:0,d:0,e:9005});return 5040.5;}});
console.log('getter changes length',sum(changing),gets,changing.length);
let converts=0;const coercion=make(96);coercion[0].a={valueOf(){converts++;coercion[1].e=7000;return 1000.5;}};
console.log('coercion changes next row',sum(coercion),converts);
const inherited=make(96);delete inherited[20].e;Object.setPrototypeOf(inherited[20],{get e(){gets++;return 7;}});gets=0;console.log('inherited field',sum(inherited),gets);
const sparse=make(96);const replacement=sparse[20];delete sparse[20];
const prototype=Object.create(Array.prototype);Object.defineProperty(prototype,'20',{get(){gets++;return replacement;}});Object.setPrototypeOf(sparse,prototype);gets=0;console.log('inherited index',sum(sparse),gets);
const indexed=make(96);Object.defineProperty(indexed,'20',{get(){gets++;return replacement;}});gets=0;console.log('indexed getter',sum(indexed),gets);
const frozen=make(96);for(const row of frozen)Object.freeze(row);Object.freeze(frozen);console.log('frozen',sum(frozen),typed(frozen));
let proxyGets=0;const proxy=new Proxy(make(96),{get(target,key,receiver){proxyGets++;return Reflect.get(target,key,receiver);}});console.log('proxy',sum(proxy),proxyGets);
const alternating=Array.from({length:96},(_,i)=>JSON.parse(i%2?'[{"e":5,"d":4,"c":3,"b":2,"a":1}]':'[{"a":1,"b":2,"c":3,"d":4,"e":5}]')[0]);console.log('different layouts',sum(alternating),typed(alternating));
console.log('ordinary objects',sum(Array.from({length:96},()=>({a:1,b:2,c:3,d:4,e:5}))));
const unicode:any[]=JSON.parse('['+Array.from({length:96},()=>'{"α":1000,"é":2000,"变量":3000,"d":4,"e":5}').join(',')+']');
function unicodeSum(rows:any[]):number{let total=0;for(let i=0;i<rows.length;i++)total+=rows[i].α+rows[i].é+rows[i].变量;return total;}
console.log('unicode',unicodeSum(unicode));
function zero(rows:any[]):number{let total=0;for(let i=0;i<0;i++)total+=rows[i].a;return total;}
function skipped(rows:any[]):number{let total=0;for(let i=64;i<32;i++)total+=rows[i].a;return total;}
console.log('never reads array',zero(undefined as any),skipped(undefined as any));
function negativeZero(rows:any[]):number{let total=0;for(let i=-0;i<rows.length;i++)total+=rows[i].a/i;return total;}
console.log('negative zero counter',negativeZero(make(96)));
try{literal(make(32));}catch(error){console.log('literal out of bounds',(error as Error).name);}

function skippedUndefined(rows:any[]):number{let total:number=undefined as any;for(let i=200;i<rows.length;i++)total+=rows[i].a;return total;}
console.log('skipped undefined',skippedUndefined(make(96))===undefined);
function observesArguments(rows:any[],seed:number):any{for(let i=0;i<rows.length;i++)seed+=rows[i].a;return arguments[1];}
console.log('arguments',observesArguments(make(96),77));
function observesAliasArguments(rows:any[],row:any):any{let total=0;for(let i=0;i<rows.length;i++){var row=rows[i];total+=row.a;}return arguments[1];}
console.log('alias arguments',observesAliasArguments(make(96),88));
function escapedAlias(rows:any[]):any{var last:any;let total=0;for(let i=0;i<rows.length;i++){var last=rows[i];total+=last.a;}return last;}
console.log('escaped alias',escapedAlias(data)===data[data.length-1]);
function signedSum(rows:any[]):number{let total=-0;for(let i=0;i<rows.length;i++)total+=rows[i].a;return total;}
const signed=make(96);for(const row of signed)row.a=-0;console.log('signed sum',Object.is(signedSum(signed),-0));
