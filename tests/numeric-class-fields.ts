export {};
class Point {x=1;y=2;}
const structural:{x:any}=new Point();console.log(structural.x);
structural.x='changed';console.log(structural.x);
console.log(JSON.stringify(Object.getOwnPropertyDescriptor(new Point(),'x')));
console.log(Reflect.get(new Point(),'x'),Object.hasOwn(new Point(),'x'));
function show(p:Point) {console.log(p.x,p.y,JSON.stringify(p),Object.keys(p).join(','));}
let p=new Point();show(p);p.x=3;show(p);
let a:any=new Point();a.x=undefined;console.log(a.x,Object.hasOwn(a,'x'));show(a);
a=new Point();a.extra=4;show(a);console.log(a.extra);
a=new Point();delete a.x;console.log(a.x,Object.hasOwn(a,'x'));a.x=8;show(a);
a=new Point();Object.defineProperty(a,'x',{get(){return 12;},configurable:true});show(a);
a=new Point();Object.freeze(a);try{a.x=9;}catch{console.log('frozen');}show(a);
a=new Point();Object.preventExtensions(a);try{a.z=9;}catch{console.log('sealed');}show(a);
a=new Point();a.x='text';console.log(a.x,JSON.stringify(a));
a=new Point();const s=Symbol('s');a[s]=9;console.log(a[s],Reflect.ownKeys(a).length);show(a);
a=new Point();console.log(JSON.stringify({...a}),Object.values(a).join(','),Object.entries(a).length);
class Ordered {declare a:number;declare b:number;constructor(){this.b=2;this.a=1;}}
console.log(Object.keys(new Ordered()).join(','));

let receiver:any={};let point=new Point();
console.log(Reflect.set(point,'x',17,receiver),point.x,receiver.x);
console.log(Reflect.get(point,'y',{}));
Object.defineProperty(Point.prototype,'x',{get(){return 40;},set(v){console.log('prototype setter',v);},configurable:true});
point=new Point();console.log(point.x,Object.hasOwn(point,'x'));
class Absent {declare value:number;}
let absent:any=new Absent();console.log(absent.value,Object.hasOwn(absent,'value'));
absent.value=undefined;console.log(absent.value,Object.hasOwn(absent,'value'));
delete absent.value;Object.defineProperty(Absent.prototype,'value',{get(){return 41;},configurable:true});
console.log(absent.value);
class Optional {value:number|undefined=undefined;}
let optional=new Optional();console.log(optional.value,Object.hasOwn(optional,'value'));
class Mixed {x=1;name='mixed';}
console.log(new Mixed().x,new Mixed().name);
class WithMethod {x=1;read(){return this.x;}}
console.log(new WithMethod().read());
export function work(count:number,offset:number):number {
  class Value {value=offset;constructor(value:number){this.value=value;}}
  const values:Value[]=[];
  for(let i=0;i<count;i++)values.push(new Value(i));
  let sum=0;for(let i=0;i<values.length;i++)sum+=values[i].value;
  return sum;
}
console.log(work(10,2));
