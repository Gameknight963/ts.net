const own:any={b:2,10:10,2:2,a:1};
for(const key in own)console.log('own',key,own[key]);
const protoParent={shared:1,inherited:3};
const child:any={own:2,shared:4};child.__proto__=protoParent;
for(let key in child)console.log('inherited',key,child[key]);
const callbacks:(()=>string)[]=[];
for(let key in own)callbacks.push(()=>key);
console.log('closures',callbacks[0](),callbacks[1](),callbacks[2](),callbacks[3]());
const vars:(()=>string)[]=[];
var key="";
for(key in own)vars.push(()=>key);
console.log('var',key,vars[0](),vars[3]());
let assigned='';for(assigned in {x:1,y:2})console.log('assigned',assigned);
const holder={key:''};for(holder.key in {p:1,q:2})console.log('property',holder.key);
const sparse:number[]=[];sparse[3]=30;sparse[9]=90;
(sparse as any).extra=4;
for(const key in sparse)console.log('sparse',key,(sparse as any)[key]);
for(const key in ('A??B' as any))console.log('string',key);
for(const value of [null,undefined,1,true] as any[])for(const key in value)console.log('unexpected',key);
let count=0;
outer:for(const x in own){for(const y in own){if(y==='10')continue outer;count++;}}
console.log('labels',count);
let getter=0;const accessors={get value():number{getter++;return 3;}};
for(const key in accessors)console.log('getter',key,getter);
class Parent {a=1;hidden():number{return 1;}}
class Child extends Parent {b=2;override hidden():number{return 2;}}
(Parent.prototype as any).visible=3;
for(const key in new Child())console.log('class',key);
const removed=[1,2,3];for(const key in removed){console.log('removed',key);if(key==='0')removed.pop();}
const growing:any={a:1,b:2};for(const key in growing){console.log('growing',key);growing.c=3;}
function lexicalSource(read:()=>string):any {try {read();}catch(error){console.log('tdz',error instanceof ReferenceError);}return {only:1};}
for(let name in lexicalSource(():string=>name))console.log('lexical',name);
let evaluations=0;
function source():any {evaluations++;return {first:1,second:2};}
for(const key in source()){try {console.log('finally-key',key);if(key==='first')continue;break;}finally {console.log('finally');}}
console.log('evaluated-once',evaluations);
const shadowParent:any={hidden:1,constructor:2,length:3,name:4,prototype:5};
function Hidden(this:any){}
Hidden.prototype=shadowParent;
class HideMethod extends (Hidden as any) {hidden():number{return 1;}}
for(const key in new HideMethod())console.log('hidden-shadow',key);
const functionValue:any=function named(){};
functionValue.__proto__=shadowParent;
for(const key in functionValue)console.log('function-shadow',key);
const arrayShadow:any=[];arrayShadow.__proto__=shadowParent;
for(const key in arrayShadow)console.log('array-shadow',key);

const huge:any[]=[];huge[2147483648]='large';huge[4294967294]='last';huge[1]=undefined;
for(const key in huge)console.log('large-index',key,huge[key as any]);
(Number.prototype as any).visibleNumber=23;
for(const key in (4 as any))console.log('boxed-number',key);
class StaticParent {static parentKey=1;static common=2;}
class StaticChild extends StaticParent {static childKey=3;static override common=4;}
for(const key in StaticChild)console.log('static-class',key);
const boundStatic=StaticChild.bind(null);
for(const key in boundStatic)console.log('bound-static',key);
const arrowWithPrototype:any=()=>1;arrowWithPrototype.prototype={};
for(const key in arrowWithPrototype)console.log('arrow-own-key',key);
const boundWithPrototype:any=Hidden.bind(null);boundWithPrototype.prototype={};
for(const key in boundWithPrototype)console.log('bound-own-key',key);
