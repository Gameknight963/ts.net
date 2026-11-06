console.log('start');
async function sum():Promise<number> {return 1+await Promise.resolve(2);}
sum().then(value=>console.log('sum',value));
async function both():Promise<number> {return await 3 + await 4;}
both().then(value=>console.log('both',value));
async function unary():Promise<boolean> {return !(await false);}
unary().then(value=>console.log('unary',value));
async function asserted():Promise<number> {return (await Promise.resolve(5)) as number;}
asserted().then(value=>console.log('asserted',value));
async function choice(flag:boolean):Promise<number> {return flag ? await 6 : await 7;}
choice(true).then(value=>console.log('choice true',value));choice(false).then(value=>console.log('choice false',value));
async function logical(first:number):Promise<number> {return (first && await 8) + 1;}
logical(0).then(value=>console.log('logical zero',value));logical(1).then(value=>console.log('logical one',value));
function add(a:number,b:number):number {return a+b;}
async function call():Promise<number> {return add(10,await 2);}
call().then(value=>console.log('call',value));
const object={offset:20,add(this:{offset:number},value:number):number {return this.offset+value;}};
async function method():Promise<number> {return object.add(await 3);}
method().then(value=>console.log('method',value));
async function property():Promise<number> {return (await Promise.resolve(object)).offset;}
property().then(value=>console.log('property',value));
async function index():Promise<number> {return [30,31][await 1];}
index().then(value=>console.log('index',value));
console.log('end');
async function nullish(value:number|null|undefined):Promise<number> {return (value??await 40)+1;}
nullish(null).then(value=>console.log('nullish null',value));nullish(0).then(value=>console.log('nullish zero',value));
async function array():Promise<(number|undefined)[]> {return [1,await 2,,await 3];}
array().then(value=>console.log('array',value.join(','),Object.keys(value).join(',')));
async function literal():Promise<{first:number,second:number}> {return {first:await 4,second:5};}
literal().then(value=>console.log('literal',value.first,value.second));
function collect(...values:number[]):number {return values.reduce((a,b)=>a+b,0);}
const spread=[1,2];
async function spreadCall():Promise<number> {return collect(...spread,await Promise.resolve().then(()=>{spread[0]=99;return 3;}));}
spreadCall().then(value=>console.log('spread snapshot',value,spread[0]));
const source={get value():number {console.log('spread getter');return 6;}};
async function objectSpread():Promise<{value:number,second:number}> {return {...source,second:await 7};}
objectSpread().then(value=>console.log('object spread',value.value,value.second));
class Item {constructor(public value:number){}}
async function construct():Promise<Item> {return new Item(await 8);}
construct().then(value=>console.log('construct',value instanceof Item,value.value));
const captured={base:10,get fn(): (this:{base:number},x:number)=>number {console.log('get fn');return function(this:{base:number},x:number):number{return this.base+x;};}};
async function savedMethod():Promise<number>{return captured.fn(await Promise.resolve().then(()=>{console.log('arg ready');return 2;}));}
savedMethod().then(v=>console.log('saved method',v));
const key={toString():string {console.log('key converted');return 'answer';}};
async function keyed():Promise<any>{return {[key as any]:await Promise.resolve().then(()=>{console.log('key value');return 42;})};}
keyed().then(v=>console.log('keyed',v.answer));
function fail(value:number):number {throw new Error('after '+value);}
async function throwing():Promise<number>{return fail(await 9);}
throwing().catch(e=>console.log('rejected',e.message));
async function rejectedOperand():Promise<number>{return 1+await Promise.reject('operand');}
rejectedOperand().catch(e=>console.log('rejected operand',e));
async function or(value:number):Promise<number>{return (value || await 11)+1;}
or(0).then(v=>console.log('or zero',v));or(5).then(v=>console.log('or five',v));
async function nested(flag:boolean):Promise<number>{return (flag?await 2:await 3)*(await 4);}
nested(true).then(v=>console.log('nested true',v));nested(false).then(v=>console.log('nested false',v));
async function capturedLocal(value:number):Promise<number>{let local=value+1;return local+await value;}
capturedLocal(7).then(v=>console.log('local',v));
