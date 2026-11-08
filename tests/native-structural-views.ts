export {};
interface Sized {readonly size:number;}
interface OtherSize {size:number;}
function size(value:Sized):number{return value.size;}
function forward(value:Sized):Sized{return value;}
function again(value:OtherSize):number{return value.size;}
function relay(value:Sized):number{return again(value);}
const map=new Map([["a",1]]);const set=new Set([1,2]);
console.log("native",size(map),size(set),forward(map)===map,relay(map));
console.log("ordinary",size({size:3}),relay({size:4}));
interface Length {readonly length:number;}
function length(value:Length):number{return value.length;}
console.log("length",length("hello"),length([1,2]),length(new Uint8Array(3)));
interface DateView {getTime():number;}
function time(value:DateView):number{return value.getTime();}
console.log("date",time(new Date(1234)));
interface Pattern {test(value:string):boolean;}
function test(value:Pattern):boolean{return value.test("hello");}
console.log("regexp",test(/ell/));
interface Named {readonly name:string;}
function name(value:Named):string{return value.name;}
function callback():void{}
console.log("function",name(callback));
function returned():Sized{return map;}
async function asynchronous():Promise<Sized>{return set;}
async function main():Promise<void>{console.log("returns",returned()===map,await asynchronous()===set,size(await asynchronous()));}
main();
const box:{value:Sized}={value:map};const list:Sized[]=[map,set];
console.log("containers",size(box.value),list.map(size).join(","));
let assigned:Sized={size:0};assigned=map;console.log("assignment",size(assigned));
const asserted=map as Sized;console.log("assertion",asserted===map,size(asserted));

interface Formatter {toFixed(digits?:number):string;}
function format(value:Formatter):string{return value.toFixed(2);}
console.log("number",format(1.25));
class SpecialDate extends Date{label="special";}
console.log("native subclass",time(new SpecialDate(4321)));

const inferred=[map,set];function total(values:readonly Sized[]):number{return values.reduce((n,v)=>n+v.size,0);}
console.log("reused array",total(inferred));
const holder={item:map};function held(value:{item:Sized}):number{return value.item.size;}
console.log("reused holder",held(holder));
function produced(make:()=>Sized):number{return make().size;}
console.log("callback return",produced(()=>map));
function accept(value:Sized):void{console.log("callback input",value.size);}
function invoke(callback:(value:Map<string,number>)=>void):void{callback(map);}
invoke(accept);
function nestedMap(value:ReadonlyMap<string,Sized>):number{return value.get("value")!.size;}
console.log("generic container",nestedMap(new Map([["value",map]])));

interface Counted {readonly size:number;}
function constrained<T extends Counted>(value:T):Counted{return value;}
console.log("generic constraint",constrained(map).size);
const tagged=Object.assign(map,{label:"map"});
interface TaggedSize {readonly size:number;}
function taggedSize(value:TaggedSize):number{return value.size;}
console.log("intersection",taggedSize(tagged));
