let effects=0;
function effect():number {console.log('effect',++effects);return 0;}
async function index(value:number[]|null):Promise<number|undefined>{return value?.[await effect()];}
index(null).then(v=>console.log('missing index',v,effects));index([10]).then(v=>console.log('index',v));
const object={base:20,method(this:{base:number},x:number):number{return this.base+x;}};
async function method(value:typeof object|null):Promise<number|undefined>{return value?.method(await effect());}
method(null).then(v=>console.log('missing method',v));method(object).then(v=>console.log('method',v));
async function optionalCall(value:((x:number)=>number)|undefined):Promise<number|undefined>{return value?.(await effect());}
optionalCall(undefined).then(v=>console.log('missing call',v));optionalCall(x=>x+30).then(v=>console.log('call',v));
async function base():Promise<number|undefined>{return (await Promise.resolve(object))?.method(await 2);}
base().then(v=>console.log('base',v));
async function parenthesized():Promise<number>{return (object?.method)(await 3);}
parenthesized().then(v=>console.log('parenthesized',v));
async function parenthesizedOptional():Promise<number|undefined>{return (object?.method)?.(await 4);}
parenthesizedOptional().then(v=>console.log('parenthesized optional',v));
async function nested(value:any):Promise<any>{return value?.next?.[await effect()]?.(await effect());}
nested(null).then(v=>console.log('nested missing',v));nested({next:[(x:number)=>40+x]}).then(v=>console.log('nested',v));
let current=(x:number)=>50+x;
const getter={get method():(x:number)=>number{console.log('getter');return current;}};
async function captured():Promise<number>{return getter.method?.(await Promise.resolve().then(()=>{current=x=>999;console.log('mutate');return 6;}))!;}
captured().then(v=>console.log('captured',v));
const throwing:any={get method():any{console.log('throwing getter');throw new Error('getter failed');}};
async function getterFailure():Promise<any>{return throwing?.method(await effect());}
getterFailure().catch(e=>console.log('getter failure',e.message));
async function notCallable():Promise<any>{const object:any={method:1};return object?.method(await Promise.resolve().then(()=>{console.log('noncallable argument');return 0;}));}
notCallable().catch(e=>console.log('noncallable',e instanceof TypeError));
async function missingBase():Promise<any>{return (await Promise.resolve(null as any))?.[await effect()];}
missingBase().then(v=>console.log('missing base',v));
async function rejection():Promise<any>{return object?.method(await Promise.reject('argument failed'));}
rejection().catch(e=>console.log('rejection',e));
async function surrounding(value:any):Promise<number>{return (value?.[await 0]??await 7)+(await 1);}
surrounding(null).then(v=>console.log('surrounding missing',v));surrounding([8]).then(v=>console.log('surrounding',v));
function add(...values:number[]):number{return values.reduce((a,b)=>a+b,0);}
async function spread():Promise<number|undefined>{return add?.(...[1,2],await 3);}
spread().then(v=>console.log('spread',v));
