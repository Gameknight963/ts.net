export {};
const object={value:4};const array:any=[1,2];array.value=5;const functionValue:any=()=>7;functionValue.value=6;const map:any=new Map([["a",1]]);map.value=8;
for(const replacement of [object,array,functionValue,map]){
 class Base{value=0;constructor(){return replacement as any;}}
 class Derived extends Base{extra=9;#secret=10;read=()=>this.#secret;}
 const value=new Derived();console.log("replacement",value===replacement,value.value,value.extra,value.read(),value instanceof Derived);
}
class Nil extends null{constructor(){return Object.create(null);}}
console.log("null base",Object.getPrototypeOf(new Nil())===null);

{
function returned(first:number,second:number):IArguments{return arguments;}
const replacement=returned(1,2);
class Base {constructor(){return replacement as any;}}
class Derived extends Base {0=7;length=1;extra=9;#secret=10;read=()=>this.#secret;}
const value:any=new Derived();
console.log('returned arguments fields',value===replacement,value[0],value[1],value.length,value.extra,value.read());
console.log('returned arguments keys',Object.keys(value).join(','),Object.getOwnPropertyNames(value).join(','));
console.log('returned arguments iteration',[...value].join(','));
const descriptor=Object.getOwnPropertyDescriptor(value,'length')!;
console.log('returned arguments descriptor',descriptor.value,descriptor.writable,descriptor.enumerable,descriptor.configurable);
}
