export {};
interface Lookup<K,V> extends ReadonlyMap<K,V>{}
interface Nested<K,V> extends Lookup<K,V>{label?:string;}
interface Mutable<K,V> extends Map<K,V>{}
function lookup(value:Lookup<string,number>):number{return value.get("x")??0;}
function forward<K,V>(value:Nested<K,V>):Nested<K,V>{return value;}
function add(value:Mutable<string,number>):void{value.set("y",9);}
interface Unique extends ReadonlySet<string>{}
function unique(value:Unique):string{return Array.from(value).join(",")+":"+value.has("a")+":"+value.size;}
interface Values<T> extends IterableIterator<T>{}
function iterator<T>(value:Values<T>):string{return Array.from(value).join(",");}
const map=new Map([["x",7]]);
console.log("map",lookup(map),forward(map)===map,forward(map).size);
add(map);console.log("mutable",map.get("y"),map.size);
console.log("set",unique(new Set(["a","b","a"])));
console.log("iterator",iterator([1,2,3].values()),iterator(map.keys()));
interface Sequences<T> extends ArrayLike<T>{label:string;}
function count(value:Sequences<number>):number{return value.length+value[0];}
const labeled=Object.assign(new Uint8Array([4,5]),{label:"bytes"});
console.log("extra required field",count(labeled),labeled.label);
class Numbers extends Map<string,number>{label="subclass";}
const numbers=new Numbers([["x",11]]);console.log("subclass",lookup(numbers),numbers.label,forward(numbers)===numbers);
namespace User {
 export interface Map { count:number; }
 export function read(value:Map):number{return value.count;}
}
console.log("user name",User.read({count:13}));
