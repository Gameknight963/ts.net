class Counter {
    value=1;
    optional?:number;
    static total=0;
    constructor(start=2){this.value=start;Counter.total++;}
    next(amount=1):number {this.value+=amount;return this.value;}
    capture():()=>number{return ()=>this.value;}
    static count():number{return this.total;}
}
const a=new Counter();const b=new Counter(10);
console.log(a.value,b.value,a.next(),b.next(2),Counter.total,Counter.count());
console.log(a instanceof Counter,({}) instanceof Counter,a.next===b.next,Counter.length,Counter.name);
console.log(Object.keys(a).length,Object.hasOwn(a,'optional'),a.optional===undefined,Object.hasOwn(a,'next'));
console.log(Object.keys(Counter).length,Object.keys(Counter.prototype).length,a.constructor===Counter);
const capture=a.capture();a.value=20;console.log(capture());
interface HasValue {value:number;next(amount?:number):number}
const structural:HasValue=a;console.log(structural.next(2),structural.value);
class Person {constructor(public name:string,readonly age=4){} greet():string{return this.name+this.age;}}
const person=new Person('Ada');console.log(person.name,person.age,person.greet(),Person.length);
function local(value:number){return class {field=value;read(){return this.field;}};}
const Local=local(7);const localInstance=new Local();console.log(localInstance.read(),localInstance instanceof Local);
const Named=class Inner {static self=Inner;field=Inner;};const named=new Named();console.log(Named.self===Named,named.field===Named,Named.name);
class Box<T> {constructor(public value:T){} get():T{return this.value;} set(value:T):void{this.value=value;}}
const numberBox=new Box<number>(3);const stringBox=new Box<string>('x');
numberBox.set(4);stringBox.set('y');console.log(numberBox.get(),stringBox.get());
const boxedShape:{value:number}=numberBox;console.log(boxedShape.value);boxedShape.value=9;console.log(numberBox.value);
let order='';function mark(value:string):string{order+=value;return value;}
class Ordered {field=mark('F');constructor(value=mark('P')){mark('B');}static first=mark('S');}
new Ordered();console.log(order);
function finallyClass():number {try{return 1;}finally{class FinallyLocal{value=2;}console.log(new FinallyLocal().value);}}
console.log(finallyClass());
class WithStaticBlock {static field=2;static {this.field+=3;}static after=this.field+1;}
console.log(WithStaticBlock.field,WithStaticBlock.after,Object.keys(WithStaticBlock).length);
const ClassAlias:any=Counter;
try{ClassAlias();}catch(error){console.log(error instanceof TypeError);}
let blocked=false;try{ClassAlias.prototype={};}catch(error){blocked=error instanceof TypeError;}console.log(blocked);
const detachedMethod=a.next;
try{detachedMethod();}catch(error){console.log(error instanceof TypeError);}
class Throwing {field=1;constructor(){throw new Error('constructor');}}
try{new Throwing();}catch(error){console.log((error as Error).message);}
class Returning {value=1;constructor(){return {value:9};}}
const returned=new Returning();console.log(returned.value,returned instanceof Returning);
for(let index=0;index<2;index++){class Captured {value=index;}console.log(new Captured().value);}

class PrimitiveClass {constructor(public value:number){}valueOf():number{return this.value;}toString():string{return "key"+this.value;}}
const primitiveClass=new PrimitiveClass(42);console.log(+primitiveClass,primitiveClass.toString());
const dictionary:any={};dictionary[primitiveClass as any]=7;console.log(dictionary.key42);

class ArrowClass {value=3;read=()=>this.value;}
const arrowFirst=new ArrowClass(),arrowSecond=new ArrowClass();const arrowRead=arrowFirst.read;arrowFirst.value=5;
console.log(arrowRead(),arrowFirst.read===arrowSecond.read,arrowRead.name);
const Inferred=(class {value=2;});const holder={Inner:class {value=3;}};
console.log(Inferred.name,holder.Inner.name,new Inferred().value,new holder.Inner().value);
class SelfStatic {static self=SelfStatic;}
console.log(SelfStatic.self===SelfStatic);
