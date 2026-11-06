enum Extended { A=1 }
namespace Extended {
 export let count=2;
 let secret=10;
 export function next():number {count++;return secret+count+Extended.A;}
 export function change(value:number):void {count=value;}
 export class Box {value:number;constructor(){this.value=count;}}
 console.log('body',count,next());
}
const alias=Extended;
console.log(Extended.A,Extended.count,Extended.next(),new Extended.Box().value);
Extended.count=20;
console.log(Extended.next(),alias.count);
namespace Extended {
 export const label='extended';
 export function total():number{return count+Extended.A;}
}
console.log(Extended.label,Extended.total());
Extended.change(4);
console.log(Extended.total(),alias.next());
enum ProtoNumber {__proto__=1,After=2}
enum ProtoText {__proto__='text'}
console.log(typeof ProtoNumber.__proto__,typeof ProtoText.__proto__,Object.hasOwn(ProtoNumber,'__proto__'),ProtoNumber[1],Object.keys(ProtoNumber).length);
console.log((ProtoNumber.__proto__ as any)===(ProtoText.__proto__ as any));
let initialization='';
function protoValue():number {initialization+='p';return 9;}
enum ProtoComputed {__proto__=protoValue(),Next=10}
console.log(initialization,ProtoComputed[9],typeof ProtoComputed.__proto__,Object.hasOwn(ProtoComputed,'__proto__'));
const enumParent={inherited:42};
enum ProtoObject {__proto__=enumParent as any}
console.log((ProtoObject as any).inherited,(ProtoObject.__proto__ as any)===enumParent,Object.hasOwn(ProtoObject,'inherited'));
enum ProtoNull {__proto__=null as any}
console.log(ProtoNull.__proto__,Object.hasOwn(ProtoNull,'__proto__'),(ProtoNull as any)['null']);
(ProtoNull as any).__proto__=5;
console.log(ProtoNull.__proto__,Object.hasOwn(ProtoNull,'__proto__'));


enum Forward {A=1}
namespace Forward {export function read():number{return later;}}
namespace Forward {export let later=7;}
console.log(Forward.read());
Forward.later=8;
console.log(Forward.read());

const getterParent={get inherited():number {return (this as any).Base;}};
enum ProtoAccessor {__proto__=getterParent as any,Base=8}
console.log((ProtoAccessor as any).inherited);
