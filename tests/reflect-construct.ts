function Target(this:any,value:number):void {this.value=value;this.seen=new.target;}
function Alternate():void {}
const direct=Reflect.construct(Target,[3]);console.log('ordinary',direct.value,direct.seen===Target,Object.getPrototypeOf(direct)===Target.prototype);
const changed=Reflect.construct(Target,[4],Alternate);console.log('alternate',changed.value,changed.seen===Alternate,Object.getPrototypeOf(changed)===Alternate.prototype,changed instanceof Alternate,changed instanceof Target);
class Base {base=1;seen:any;constructor(public value:number){this.seen=new.target;}}
class Derived extends Base {extra=2;before:any;constructor(value:number){const seen=new.target;super(value);this.before=seen;}}
class DefaultDerived extends Base {extra=3;}
for(const Constructor of [Base,Derived,DefaultDerived]) {
    const value=Reflect.construct(Constructor,[5],Alternate);console.log('class',value.base,value.value,value.extra,value.seen===Alternate,value.before===Alternate,Object.getPrototypeOf(value)===Alternate.prototype);
}
const bound=Target.bind(null,6);
const fromBound=Reflect.construct(bound,[]);console.log('bound',fromBound.value,fromBound.seen===Target,Object.getPrototypeOf(fromBound)===Target.prototype);
const alternateBound=Reflect.construct(bound,[],Alternate);console.log('bound alternate',alternateBound.value,alternateBound.seen===Alternate,Object.getPrototypeOf(alternateBound)===Alternate.prototype);
function Returns(this:any):any {return {replacement:true,seen:new.target};}
const replaced=Reflect.construct(Returns,[],Alternate);console.log('replacement',replaced.replacement,replaced.seen===Alternate,Object.getPrototypeOf(replaced)===Object.prototype);
for(const Constructor of [Number,String,Boolean,Array,Map,Set,WeakMap,WeakSet,Error]) {
    const value=Reflect.construct(Constructor,[],Alternate);console.log('native',Object.getPrototypeOf(value)===Alternate.prototype,value instanceof Alternate,Object.prototype.toString.call(value));
}
const ignored={value:9};const object=Reflect.construct(Object,[ignored],Alternate);console.log('object alternate',object!==ignored,Object.getPrototypeOf(object)===Alternate.prototype);
const fallback:any=function Fallback():void {};fallback.prototype=3;
const boxed=Reflect.construct(Number,[7],fallback);console.log('native fallback',Object.getPrototypeOf(boxed)===Number.prototype,Number.prototype.valueOf.call(boxed));
const plain=Reflect.construct(Target,[8],fallback);console.log('ordinary fallback',Object.getPrototypeOf(plain)===Object.prototype,plain.value,plain.seen===fallback);
let effects='';
const args={get length():number {effects+='L';return 1;},get 0():number {effects+='A';return 9;}};
console.log('argument reads',Reflect.construct(Target,args).value,effects);
for(const target of [null,{},()=>1,Math.abs]) {effects='';try{Reflect.construct(target as any,args);}catch(error){console.log('invalid target',(error as Error).name,effects);}}
for(const newTarget of [null,undefined,{},()=>1,Math.abs]) {effects='';try{Reflect.construct(Target,args,newTarget as any);}catch(error){console.log('invalid newTarget',(error as Error).name,effects);}}
for(const args of [null,undefined,'text',3])try{Reflect.construct(Target,args as any);}catch(error){console.log('invalid arguments',(error as Error).name);}
const observed:any=Alternate.bind(null);
const observedPrototype={marker:true};
const inherited=Object.create(Object.getPrototypeOf(observed),{prototype:{get(){effects+='P';return observedPrototype;}}});
Object.setPrototypeOf(observed,inherited);
class TimingBase {constructor(){effects+='B';console.log('base target',new.target===observed);}}
class TimingDerived extends TimingBase {constructor(){effects+='D';super();effects+='E';}}
effects='';const timed=Reflect.construct(TimingDerived,[],observed);console.log('derived order',effects,Object.getPrototypeOf(timed)===observedPrototype);
effects='';const converted=Reflect.construct(Number,[{valueOf():number{effects+='V';return 4;}}],observed);console.log('number order',effects,Number.prototype.valueOf.call(converted));
effects='';try{Reflect.construct(Promise,[3],observed);}catch(error){console.log('promise validation',(error as Error).name,effects);}
const descriptor=Object.getOwnPropertyDescriptor(Reflect,'construct')!;console.log('metadata',descriptor.value.name,descriptor.value.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);
