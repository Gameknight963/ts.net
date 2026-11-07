export {};
function identity<T>(value:T):T{return value;}
function pick<T,K extends keyof T>(object:T,key:K):T[K]{return object[key];}
function size<T extends {length:number}>(value:T):number{return value.length;}
class Box<T>{constructor(public value:T){} map<U>(callback:(value:T)=>U):Box<U>{return new Box(callback(this.value));}}
const numberBox=new Box(42);const stringBox=numberBox.map(value=>String(value));
console.log("box",numberBox.value,stringBox.value,typeof numberBox.value,typeof stringBox.value);
console.log("identity",identity(12),identity("hi"),identity({x:3}).x);
console.log("indexed",pick({x:4,y:"yes"},"x"),pick({x:4,y:"yes"},"y"));
console.log("constraint",size("abc"),size([1,2]),size({length:5}));
function readonly<T>(value:T):Readonly<T>{return value;}
const frozen=readonly({x:9,y:"mapped"});console.log("mapped",frozen.x,frozen.y);
type Nullable<T>={[K in keyof T]:T[K]|null};const optional:Nullable<{x:number,y:string}>={x:null,y:"text"};console.log("nullable mapped",optional.x,optional.y);
function first<T extends readonly unknown[]>(value:T):T[0]{return value[0];}console.log("tuple",first([1,"x"] as const),first(["a",2] as const));

class List<T> extends Box<T[]>{append(value:T):void{this.value.push(value);}}
const list=new List<number>([1,2]);list.append(3);console.log("generic inheritance",list.value.join("|"),list instanceof Box);
function fold<T,U>(values:T[],initial:U,combine:(acc:U,value:T)=>U):U{let result=initial;for(const value of values)result=combine(result,value);return result;}
console.log("generic callbacks",fold([1,2,3],0,(a,b)=>a+b),fold([1,2,3],"",(a,b)=>a+String(b)));
function create<T>(C:new(value:number)=>T):T{return new C(7);}console.log("generic constructor",create(Box<number>).value);
function wrap<T>(value:T):{value:T}{return {value};}console.log("generic shape",wrap(8).value,wrap("shape").value);
type Flatten<T>=T extends (infer U)[]?U:T;
function flatten<T>(value:T):Flatten<T>{return (Array.isArray(value)?value[0]:value) as Flatten<T>;}
console.log("conditional",flatten([9]),flatten("scalar"));

const numericIdentity=identity<number>;const textualIdentity=identity<string>;
console.log("instantiate",numericIdentity(13),textualIdentity("specialized"),numericIdentity===identity,numericIdentity.name,numericIdentity.length);
const NumberBox=Box<number>;console.log("constructor alias",new NumberBox(14).value,NumberBox===Box,new (Box<number>)(15).value);
let calls=0;function getIdentity(){calls++;return identity;}const selected=getIdentity()<number>;console.log("evaluate once",calls,selected(16),selected(17),calls);
const registry={identity};const fromProperty=registry.identity<string>;console.log("property",fromProperty("property"));
