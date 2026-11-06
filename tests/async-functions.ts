console.log('start');
async function value():Promise<number> {console.log('body');return 3;}
const first=value();console.log('promise',first instanceof Promise);first.then(result=>console.log('value',result));
async function failure():Promise<number> {throw 'body failure';}
try {failure().catch(reason=>console.log('rejected',reason));console.log('no synchronous throw');} catch {console.log('unexpected throw');}
async function empty():Promise<void> {console.log('empty body');}
empty().then(result=>console.log('empty result',result===undefined));
const arrow=async (value:number):Promise<number>=>value+1;
arrow(4).then(result=>console.log('arrow',result));
const expression=async function named(value:number):Promise<number> {return value+2;};
expression(4).then(result=>console.log('expression',result));
const object={offset:7,async method(this:{offset:number},value:number):Promise<number> {console.log('method body');return this.offset+value;}};
object.method(3).then(result=>console.log('method',result));
const method=object.method;method.call({offset:9},2).then(result=>console.log('borrowed',result));
const existing=Promise.resolve(8);
async function adopt():Promise<number> {return existing;}
const adopted=adopt();console.log('new identity',adopted!==existing);adopted.then(result=>console.log('adopted',result));
async function final():Promise<number> {try {return 1;} finally {console.log('finally body');}}
final().then(result=>console.log('finally value',result));
async function finalFailure():Promise<number> {try {return 1;} finally {throw 'finally failure';}}
finalFailure().catch(reason=>console.log('finally rejection',reason));
function badDefault():number {throw 'default failure';}
async function defaults(value=badDefault()):Promise<number> {return value;}
try {defaults().catch(reason=>console.log('default rejection',reason));console.log('default no throw');} catch {console.log('unexpected default throw');}
async function argumentsValue(this:unknown,value:number):Promise<number> {console.log('arguments',arguments.length,this===undefined);return value;}
(argumentsValue as any)(5,6).then((result:number)=>console.log('argument value',result));
console.log('metadata',value.name,value.length,arrow.length,Object.hasOwn(value,'prototype'),Object.hasOwn(expression,'prototype'));
try {new (value as any)();} catch(error) {console.log('not constructor',error instanceof TypeError);}
console.log('end');
const alias=value;alias().then(result=>console.log('alias',result));
const erased:()=>void=async()=>7;(erased as any)().then((result:number)=>console.log('contextual void',result));
async function destructured({value}:{value:number}):Promise<number> {return value;}
destructured({value:9}).then(result=>console.log('destructured',result));
async function rest(...values:number[]):Promise<number> {return values.reduce((a,b)=>a+b,0);}
rest(...[1,2,3]).then(result=>console.log('spread',result));
const callbacks={offset:5,make(value:number):()=>Promise<number> {return async()=>this.offset+arguments[0];}};
callbacks.make(6)().then(result=>console.log('lexical',result));
class Base {value=4;read():number {return this.value;}}
class Derived extends Base {async readAsync():Promise<number> {return super.read()+1;}static async create():Promise<number> {return 8;}async #private():Promise<number> {return this.value+2;}call():Promise<number> {return this.#private();}}
const instance=new Derived();instance.readAsync().then(result=>console.log('super',result));Derived.create().then(result=>console.log('static',result));instance.call().then(result=>console.log('private',result));
const recursive=async function recur(count:number):Promise<number> {if(count===0)return 0;return recur(count-1).then(value=>value+1);};
recursive(3).then(result=>console.log('recursive',result));
const thenable:any={get then():unknown {console.log('async then getter');return (resolve:any)=>{console.log('async then body');resolve(12);};}};
async function returnThenable():Promise<unknown> {return thenable;}
returnThenable().then(result=>console.log('returned thenable',result));
value.bind(undefined)().then(result=>console.log('bound',result));
let synchronousArgument=false;
try {defaults(badDefault());} catch {synchronousArgument=true;}
console.log('argument throws sync',synchronousArgument);
