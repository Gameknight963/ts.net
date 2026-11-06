let seen:any;
function Legacy(this:any, value=2, ...extras:number[]) {
    this.value=value;
    this.total=value+(extras[0]??0);
    seen=this;
}
Legacy.prototype.read=function(this:any):number{return this.value;};
(Legacy as any).tag=7;
(Legacy as any).readTag=function(this:any):number{return this.tag;};
class Derived extends (Legacy as any) {
    extra=3;
    static tag=10;
    constructor(value=4){super(value,5);}
    read():number{return super.read()+this.extra;}
    static readTag():number{return super.readTag()+1;}
}
const first=new Derived();
console.log(first.read(),first.total,seen===first,first instanceof Legacy,first instanceof Derived,Derived.readTag());
class Implicit extends (Legacy as any) {extra=8;}
const implicit=new (Implicit as any)(6,9);
console.log(implicit.read(),implicit.total,implicit.extra,implicit instanceof Legacy,seen===implicit);
class Deeper extends Derived {more=11;read():number{return super.read()+this.more;}}
const deeper=new Deeper(20);
console.log(deeper.read(),deeper.total,deeper instanceof Legacy,deeper instanceof Derived,deeper instanceof Deeper,seen===deeper);
const old:any=Legacy.prototype;
Legacy.prototype={read:function(this:any):number{return 90;}};
console.log(first.read(),new Derived().read(),first instanceof Legacy,first instanceof Derived);
Legacy.prototype=old;
console.log(first instanceof Legacy);
const arrow:any=()=>1;
arrow.prototype={};
try {class Bad extends arrow{};console.log('bad-arrow');} catch(error) {console.log('arrow-rejected',error instanceof TypeError);}
const invalid:any=function(){};
invalid.prototype=3;
try {class Bad extends invalid{};console.log('bad-prototype');} catch(error) {console.log('prototype-rejected',error instanceof TypeError);}
const bound:any=Legacy.bind({value:999},12);
bound.prototype=Legacy.prototype;
class BoundDerived extends bound {extra=4;}
const fromBound=new (BoundDerived as any)(13);
console.log(fromBound.read(),fromBound.total,fromBound.extra,seen===fromBound,fromBound instanceof Legacy,fromBound instanceof bound);
function NullBase(this:any, value:number){this.value=value;}
NullBase.prototype=null;
class NullDerived extends (NullBase as any) {extra=2;}
const nullInstance:any=new (NullDerived as any)(7);
console.log('null-prototype',nullInstance.value,nullInstance.extra,'toString' in nullInstance,nullInstance instanceof NullDerived);
const method:any=({method(){}}).method;
method.prototype={};
try {class Bad extends method{};console.log('bad-method');} catch(error) {console.log('method-rejected',error instanceof TypeError);}
const boundMissing:any=Legacy.bind(null);
try {class Bad extends boundMissing{};console.log('bad-bound');} catch(error) {console.log('bound-prototype-rejected',error instanceof TypeError);}
let order='';
function Replacement(this:any){order+='b';this.discarded=true;return {returned:8};}
class Replacing extends (Replacement as any) {extra=(order+='f',5);constructor(){super();order+='c';}}
const replaced:any=new (Replacing as any)();
console.log('replacement',replaced.returned,replaced.extra,replaced.discarded,order,replaced instanceof Replacing);
function Primitive(this:any){this.value=6;return 99;}
class PrimitiveDerived extends (Primitive as any) {extra=1;}
const primitive:any=new (PrimitiveDerived as any)();
console.log('primitive-return',primitive.value,primitive.extra,primitive instanceof PrimitiveDerived);
function Throwing(this:any){this.value=1;throw new Error('base failed');}
class ThrowDerived extends (Throwing as any) {}
try {new ThrowDerived();} catch(error) {console.log((error as Error).message);}
const TypedBase=Legacy as unknown as {new(value:number):{value:number;read():number}};
class TypedDerived extends TypedBase {double():number{return this.value*2;}}
const typed=new TypedDerived(8);
console.log('typed',typed.value,typed.read(),typed.double(),typed instanceof TypedBase);
let repeated='';
function Repeat(this:any){repeated+='b';this.value=repeated.length;}
class Repeated extends (Repeat as any) {extra=(repeated+='f',1);constructor(){super();try {super();} catch(error){console.log('repeat-error',error instanceof ReferenceError);} }}
const repeat:any=new Repeated();
console.log('repeat',repeated,repeat.value,repeat.extra);
function AccessorBase(this:any){this.value=2;}
AccessorBase.prototype={
    get label():number{return (this as any).value;},
    set label(value:number){(this as any).value=value;}
};
(AccessorBase as any).__proto__={
    get inherited():number{return (this as any).tag;},
    set inherited(value:number){(this as any).tag=value;}
};
class AccessorDerived extends (AccessorBase as any) {
    static tag=6;
    update(value:number):number {super.label=value;return super.label;}
    static update(value:number):number {super.inherited=value;return super.inherited;}
}
const accessors=new AccessorDerived();
console.log('accessors',accessors.update(14),accessors.label,AccessorDerived.update(17),AccessorDerived.tag,(AccessorBase as any).tag);
class ClassBase {value:number;constructor(value:number){this.value=value;}read():number{return this.value;}}
const boundClass:any=ClassBase.bind(null,21);
boundClass.prototype=ClassBase.prototype;
class BoundClassChild extends boundClass {extra=3;}
const boundClassChild:any=new BoundClassChild();
console.log('bound-class',boundClassChild.read(),boundClassChild.extra,boundClassChild instanceof ClassBase,boundClassChild instanceof BoundClassChild);
const boundDerived:any=Derived.bind(null,2);
class BoundDerivedChild extends boundDerived {}
const boundDerivedChild:any=new BoundDerivedChild();
console.log('bound-inherited-prototype',boundDerivedChild.read(),boundDerivedChild.extra,boundDerivedChild instanceof Derived,boundDerivedChild instanceof BoundDerivedChild);
function MissingPropertyBase(this:any){}
class ShadowSetter extends (MissingPropertyBase as any) {
    hits=0;
    set shadow(value:number){this.hits+=value;}
    write():void {super.shadow=5;}
}
const shadow:any=new ShadowSetter();
shadow.write();
console.log('super-own-data',shadow.hits,shadow.shadow,Object.hasOwn(shadow,'shadow'));
class StaticShadow extends (MissingPropertyBase as any) {
    static hits=0;
    static set shadow(value:number){this.hits+=value;}
    static write():void {super.shadow=9;}
}
try {StaticShadow.write();} catch(error){console.log('static-super-own-error',error instanceof TypeError);}
console.log('static-super-own-data',StaticShadow.hits,(StaticShadow as any).shadow,Object.hasOwn(StaticShadow,'shadow'));
