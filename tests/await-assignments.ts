let local=1;
async function simple():Promise<number>{return local=await 2;}
simple().then(v=>console.log('simple',v,local));
let compound=3;
async function sum():Promise<number>{return compound+=await Promise.resolve().then(()=>{compound=100;return 4;});}
sum().then(v=>console.log('compound',v,compound));
const object={get value():number{console.log('get');return 10;},set value(v:number){console.log('set',v);}};
async function property():Promise<number>{return object.value+=await Promise.resolve().then(()=>{console.log('ready');return 5;});}
property().then(v=>console.log('property',v));
const values:any={a:20};
const key={toString():string{console.log('key');return 'a';}};
async function indexed():Promise<number>{return values[key as any]+=await Promise.resolve().then(()=>{values.a=200;console.log('index ready');return 6;});}
indexed().then(v=>console.log('indexed',v,values.a));
async function leftAwait():Promise<number>{return (await Promise.resolve(values)).b=await 8;}
leftAwait().then(v=>console.log('left await',v,values.b));
let yes=1,no=0,missing:number|undefined=undefined;
async function and():Promise<number>{return (yes&&=await 11)+1;}
async function or():Promise<number>{return (no||=await 12)+1;}
async function nullish():Promise<number>{return (missing??=await 13)+1;}
and().then(v=>console.log('and',v,yes));or().then(v=>console.log('or',v,no));nullish().then(v=>console.log('nullish',v,missing));
async function skip():Promise<number>{return (no&&=await 99)+(yes||=await 99);}
no=0;yes=1;skip().then(v=>console.log('skip',v));
async function rejected():Promise<number>{return local=await Promise.reject('no write');}
rejected().catch(v=>console.log('rejected',v,local));
let changingName='a';
const changingKey={toString():string {console.log('changing key',changingName);return changingName;}};
const changing:any={a:30,b:40};
async function changedKey():Promise<number>{return changing[changingKey as any]+=await Promise.resolve().then(()=>{changingName='b';return 2;});}
changedKey().then(v=>console.log('changed key',v,changing.a,changing.b));
let text='a';
async function append():Promise<string>{return text+=await 'b';}
append().then(v=>console.log('text',v,text));
let bits=3,power=2;
async function shift():Promise<number>{return bits<<=await 2;}
async function exponent():Promise<number>{return power**=await 3;}
shift().then(v=>console.log('shift',v,bits));exponent().then(v=>console.log('power',v,power));
async function skipNullish():Promise<number>{let present=0;return present??=await 99;}
skipNullish().then(v=>console.log('skip nullish',v));
async function simpleKey():Promise<number>{return changing[key as any]=await Promise.resolve().then(()=>{console.log('simple ready');return 9;});}
simpleKey().then(v=>console.log('simple key',v));
async function nullTarget():Promise<number>{const target:any=null;return target[key as any]=await Promise.resolve().then(()=>{console.log('null ready');return 1;});}
nullTarget().catch(e=>console.log('null target',e instanceof TypeError));
const updates=[4,8];
async function postfix():Promise<number>{return updates[await 0]++;}
async function prefix():Promise<number>{return ++updates[await 1];}
postfix().then(v=>console.log('postfix',v,updates[0]));prefix().then(v=>console.log('prefix',v,updates[1]));
class Counter {
 #value=5;
 async add():Promise<number>{return this.#value+=await 2;}
 async assign():Promise<number>{return this.#value=await 9;}
}
const counter=new Counter();counter.add().then(v=>console.log('private add',v));counter.assign().then(v=>console.log('private assign',v));
async function rejectedTarget():Promise<number>{const target:any=null;return target[key as any]+=await Promise.resolve().then(()=>{console.log('should not run');return 1;});}
rejectedTarget().catch(e=>console.log('rejected target',e instanceof TypeError));
