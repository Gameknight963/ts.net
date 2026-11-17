export {};
class Mixed {
 value=1;name='one';enabled=true;
 item:{n:number}|undefined={n:3};
 callback=()=>this.value;
 list=[1,2];
 read(){return this.name+':'+this.value;}
}
let mixed=new Mixed();
console.log(mixed.read(),mixed.enabled,mixed.item!.n,mixed.callback(),mixed.list.join(','));
mixed.name='two';mixed.value=4;mixed.enabled=false;mixed.item=undefined;
console.log(mixed.read(),mixed.enabled,mixed.item,mixed.callback(),Object.hasOwn(mixed,'item'));
let view:{name:any,value:any}=mixed;console.log(view.name,view.value);
view.name=7;console.log((view as any).name);
let a:any=new Mixed();a.extra=8;console.log(a.name,a.value,a.extra,Object.keys(a).join(','));
a=new Mixed();delete a.name;console.log(a.name,Object.hasOwn(a,'name'));a.name='again';console.log(a.name,Object.keys(a).join(','));
a=new Mixed();Object.defineProperty(a,'name',{get(){return 'getter';},configurable:true});console.log(a.read());
a=new Mixed();Object.freeze(a);try{a.name='fail';}catch{console.log('frozen');}console.log(a.name,a.value);
a=new Mixed();a.value='text';console.log(a.value,a.name);
class Nullable {value:number=1;text:string|null=null;optional?:string;declare missing:string;}
let nullable=new Nullable();console.log(nullable.text,nullable.optional,Object.hasOwn(nullable,'optional'),Object.hasOwn(nullable,'missing'));
nullable.text='defined';nullable.optional='defined';console.log(nullable.text,nullable.optional);
nullable.text=null;nullable.optional=undefined;console.log(nullable.text,nullable.optional);
class AnyField {value:any=1;name='one';}
let arbitrary=new AnyField();let numberView=arbitrary as {value:number};
console.log(numberView.value);arbitrary.value=2;console.log(numberView.value);
arbitrary.value=undefined;console.log(numberView.value,Object.hasOwn(arbitrary,'value'));
arbitrary.value=null;console.log(arbitrary.value);
class Base {value=2;name='base';read(){return this.name+this.value;}}
class Derived extends Base {extra=3;}
console.log(new Derived().read(),new Derived().extra);
class Ordered {declare first:string;declare second:number;constructor(){this.second=2;this.first='first';}}
console.log(JSON.stringify(new Ordered()));

class ReferenceBox {value=1;payload:unknown;constructor(payload:unknown){this.payload=payload;}}
export function make(value:unknown):unknown{return new ReferenceBox(value);}
export function work(count:number,offset:number):number {
 class Record {value=offset;name='item';enabled=true;constructor(value:number){this.value=value;}}
 const values:Record[]=[];
 for(let i=0;i<count;i++)values.push(new Record(i));
 let sum=0;for(let i=0;i<values.length;i++)sum+=values[i].value;
 return sum;
}
console.log(work(10,2));

export function updates(count:number):number {
 class Counter {value:any=0;}
 const target:{value:number}=new Counter() as any;
 for(let i=0;i<count;i++)target.value=i;
 return target.value;
}
console.log(updates(10));
