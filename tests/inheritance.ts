let order='';
class Base {
    value=2;
    static count=3;
    constructor(value=4){order+='B';this.value=value;}
    read(add=0):number{return this.value+add;}
    static read(add=0):number{return this.count+add;}
}
class Derived extends Base {
    extra=(order+='F',this.value+1);
    constructor(value=5,public label='x'){order+='D';super(value);order+='E';}
    override read(add=0):number{return super.read(add)+this.extra;}
    capture():()=>number{return ()=>super.read(2);}
    static override read(add=0):number{return super.read(add)+1;}
}
const value=new Derived(7,'a');console.log(value.value,value.extra,value.label,value.read(2),order);
console.log(value instanceof Base,value instanceof Derived,Derived.prototype instanceof Base,Base.prototype instanceof Base);
console.log(Derived.count,Derived.read(2),Object.hasOwn(Derived,'count'));
const read=value.capture();value.value=10;console.log(read());
class Third extends Derived {override read(add=0):number{return super.read(add)+1;}}
const third=new Third();console.log(third.read(),third.value,third.extra,third.label,third instanceof Base,third instanceof Derived);
const baseView:Base=third;console.log(baseView.read(2));
console.log(Object.keys(third).length,Object.hasOwn(third,'read'),third.constructor===Third);
class StaticDerived extends Base {static x=super.read(2);static {this.count+=1;}}
console.log(StaticDerived.x,StaticDerived.count,Base.count);
class WriteSuper extends Base {write(){super.read=()=>9;return this.read();}}
console.log(new WriteSuper().write(),Base.prototype.value===undefined);
class Conditional extends Base {constructor(ok:boolean){if(ok)super(3);}}
try{new Conditional(false);}catch(error){console.log(error instanceof ReferenceError);}
console.log(new Conditional(true).value);
class Twice extends Base {constructor(){super();super();}}
try{new Twice();}catch(error){console.log(error instanceof ReferenceError);}
class PrimitiveReturn extends Base {constructor(){super();return 3 as any;}}
try{new PrimitiveReturn();}catch(error){console.log(error instanceof TypeError);}
class BeforeSuper extends Base {constructor(read=()=>this.value){read();super();}}
try{new BeforeSuper();}catch(error){console.log(error instanceof ReferenceError);}
class BranchProperties extends Base {constructor(ok:boolean,public label='p'){super(ok?11:12);}}
console.log(new BranchProperties(true).label,new BranchProperties(false).value);
class GenericBase<T> {constructor(public stored:T){}get():T{return this.stored;}}
class NumberChild extends GenericBase<number> {twice(){return super.get()*2;}}
const generic=new NumberChild(9);console.log(generic.get(),generic.twice(),generic instanceof GenericBase);
let bases=0;function chooseBase(){bases++;return Base;}
const Dynamic=class extends chooseBase(){};const dynamic=new Dynamic(6);console.log(bases,dynamic.value,dynamic instanceof Base);
class StringBase {text='base';greet():string{return this.text;}}
class StringChild extends StringBase {override greet():string{return super.greet()+' child';}extract(){return super.greet;}}
const strings=new StringChild();console.log(strings.greet());
const looseGreet=strings.extract();try{looseGreet();}catch(error){console.log(error instanceof TypeError);}
class NullChild extends null {}
try{new NullChild();}catch(error){console.log(error instanceof TypeError);}
const invalidBase:any=1;try{class Invalid extends invalidBase{}}catch(error){console.log(error instanceof TypeError);}
let repeated=0;
class RepeatedBase {value=++repeated;}
class RepeatedChild extends RepeatedBase {constructor(){super();try{super();}catch{}console.log(this.value);}}
new RepeatedChild();console.log(repeated);
const escaped:RetryBase[]=[];
class RetryBase {value=1;constructor(fail:boolean){escaped.push(this);if(fail)throw new Error('retry');}}
class RetryChild extends RetryBase {constructor(){try{super(true);}catch{super(false);}}}
const retried=new RetryChild();console.log(escaped.length,escaped[0]===retried,escaped[1]===retried);
const undefinedBase:any=undefined;try{class InvalidUndefined extends undefinedBase{}}catch(error){console.log(error instanceof TypeError);}
class SpreadChild extends Base {constructor(...values:[number?]){super(...values);}call(...values:[number?]){return super.read(...values);}}
const spreadChild=new SpreadChild(14);console.log(spreadChild.value,spreadChild.call(2),new SpreadChild().value);
