console.log('start');
async function ready():Promise<number> {console.log('ready body');return await Promise.resolve(3);}
ready().then(value=>console.log('ready result',value));
Promise.resolve().then(()=>console.log('competing first')).then(()=>console.log('competing second'));
async function primitive():Promise<number> {return await 7;}
primitive().then(value=>console.log('primitive',value));
const arrow=async()=>await 'arrow';arrow().then(value=>console.log(value));
async function failed():Promise<number> {return await Promise.reject('await reason');}
failed().catch(reason=>console.log('rejected',reason));
async function branch(flag:boolean):Promise<number> {if(flag)return await 1;return await 2;}
branch(true).then(value=>console.log('branch true',value));branch(false).then(value=>console.log('branch false',value));
async function loop():Promise<number> {for(let i=0;i<3;i++) {if(i===2)return await i;}return 0;}
loop().then(value=>console.log('loop',value));
const thenable:any={get then():unknown {console.log('then getter');return function(this:unknown,resolve:any):void {console.log('then body',this===thenable);resolve(8);};}};
async function adopt():Promise<number> {return await thenable;}
adopt().then(value=>console.log('thenable result',value));
const native:any=Promise.resolve(9);native.then=()=>{throw 'public then must not run';};
async function intrinsic():Promise<number> {return await native;}
intrinsic().then(value=>console.log('intrinsic result',value));
const nativeConstructor:any=Promise.resolve(1);Object.setPrototypeOf(nativeConstructor,{get constructor():unknown {throw 'constructor failure';}});
async function constructorFailure():Promise<number> {return await nativeConstructor;}
constructorFailure().catch(reason=>console.log('constructor rejection',reason));
let fulfill!:(value:number)=>void;const pending=new Promise<number>(resolve=>{fulfill=resolve;});
async function later():Promise<number> {return await pending;}
later().then(value=>console.log('later result',value));queueMicrotask(()=>fulfill(11));
class Container {value=4;async read():Promise<number> {return await Promise.resolve(this.value);}}
new Container().read().then(value=>console.log('method',value));
async function parenthesized():Promise<number> {return (await 12);}
parenthesized().then(value=>console.log('parenthesized',value));
console.log('end');
async function localObject():Promise<{value:number}> {const object={value:6};return await object;}
localObject().then(value=>console.log('local object',value.value));
async function localArray():Promise<number[]> {const array=[1,2,3];return await array;}
localArray().then(value=>console.log('local array',value.join(',')));
async function recurse(count:number):Promise<number> {if(count===0)return 0;return await recurse(count-1);}
recurse(4).then(value=>console.log('recursive return',value));
async function nullValue():Promise<null> {return await null;}
async function undefinedValue():Promise<void> {return await undefined;}
nullValue().then(value=>console.log('null value',value===null));undefinedValue().then(value=>console.log('undefined value',value===undefined));
const constructorCount:any=Promise.resolve(13);let constructorReads=0;
Object.setPrototypeOf(constructorCount,{get constructor():unknown {constructorReads++;return Promise;}});
async function counted():Promise<number> {return await constructorCount;}
counted().then(value=>console.log('constructor count',value,constructorReads));
const savedThen=Promise.prototype.then;const savedResolve=Promise.resolve;
const direct=Promise.resolve(14);
(Promise.prototype as any).then=()=>{throw 'prototype then must not run';};
(Promise as any).resolve=()=>{throw 'public resolve must not run';};
async function bypass():Promise<number> {return await direct;}
const bypassResult=bypass();
Promise.prototype.then=savedThen;Promise.resolve=savedResolve;
bypassResult.then(value=>console.log('intrinsic methods',value));
function outer():Promise<number> {try {const nested=async()=>await 15;return nested();}finally {console.log('outer finally');}}
outer().then(value=>console.log('nested async',value));
