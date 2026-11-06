function Point(this:any,x:number,y=2):void {this.x=x;this.y=y;}
const Constructor:any=Point;
Constructor.prototype.sum=function(this:any):number{return this.x+this.y;};
const first=new Constructor(3),second=new Constructor(4,5);
console.log(first.x,first.y,first.sum(),second.sum(),first instanceof Constructor,first instanceof Object,first.constructor===Point,Object.keys(first).length);
console.log(first.__proto__===Constructor.prototype,Constructor.prototype.__proto__===Object.prototype);
function Replacement(this:any):object {this.discarded=1;return {value:9};}
const replace:any=Replacement;const replaced=new replace();console.log(replaced.value,replaced.discarded,replaced instanceof replace);
function Primitive(this:any,value:any):any {this.value=7;return value;}
const primitive:any=Primitive;
for(const value of [undefined,null,0,false,'text']) {const item=new primitive(value);console.log(item.value,item instanceof primitive);}
const old=Constructor.prototype;Constructor.prototype={label:'new'};
const newer=new Constructor(1);console.log(first instanceof Constructor,newer instanceof Constructor,newer.label,first.sum());
Constructor.prototype=old;console.log(first instanceof Constructor,newer instanceof Constructor);
for(const prototype of [undefined,null,0,false,'text']) {primitive.prototype=prototype;const item=new primitive();console.log(item.value,item instanceof Object,item.__proto__===Object.prototype);try{console.log(item instanceof primitive);}catch(error){console.log(error instanceof TypeError);}}
const arrow:any=()=>({value:1});try{new arrow();}catch(error){console.log(error instanceof TypeError);}
console.log((0 as any) instanceof arrow,(null as any) instanceof arrow);
const methods:any={method(){return {};}};try{new methods.method();}catch(error){console.log(error instanceof TypeError);}
const Bound:any=Constructor.bind({x:100},10);const bound=new Bound(20);
console.log(bound.x,bound.y,bound instanceof Constructor,bound instanceof Bound);
function Typed(this:{value:number},value:number):void {this.value=value;}
const TypedConstructor=Typed as unknown as {new(value:number):{value:number}};
const typed=new TypedConstructor(12);console.log(typed.value);
function Accessor(this:any):void {this.value=4;}
const access:any=Accessor;access.prototype={get doubled(){return (this as any).value*2;},set doubled(value:number){(this as any).value=value/2;}};
const accessed=new access();console.log(accessed.doubled);accessed.doubled=14;console.log(accessed.value,accessed.doubled,Object.hasOwn(accessed,'doubled'));
function ReturnFunction():any {return ()=>19;}
const returningFunction:any=ReturnFunction;const returnedFunction=new returningFunction();console.log(returnedFunction(),returnedFunction instanceof returningFunction);
class ReturnedClass {value=23;}
function ReturnClass():any{return new ReturnedClass();}
const returningClass:any=ReturnClass;const returnedClass=new returningClass();console.log(returnedClass.value,returnedClass instanceof ReturnedClass,returnedClass instanceof returningClass);
function Chain(this:any,value:number):void {this.value=value;}
const chained:any=Chain;
const grand={get doubled(){return (this as any).value*2;},set doubled(value:number){(this as any).value=value/2;}};
const chainParent:any={};chainParent.__proto__=grand;chained.prototype=chainParent;
const child=new chained(3);console.log(child.doubled);child.doubled=20;console.log(child.value,child.doubled,Object.hasOwn(child,'doubled'));
try{child.__proto__=child;}catch(error){console.log(error instanceof TypeError);}
console.log(child instanceof chained);
const readOnlyProto:any=function():void {};chained.prototype=readOnlyProto;
const readonlyChild=new chained(1);try{readonlyChild.length=5;}catch(error){console.log(error instanceof TypeError);}
const oldConstructor=chainParent.constructor;chainParent.constructor=3;console.log(chainParent.constructor===3);chainParent.constructor=oldConstructor;
const spreadArgs:[number,number]=[8,9];Constructor.prototype=old;const spread=new Constructor(...spreadArgs);console.log(spread.x,spread.y);
function Generic(this:any,value:unknown):void {this.value=value;}
const GenericConstructor=Generic as unknown as {new<T>(value:T):{value:T}};
const genericNumber=new GenericConstructor<number>(14),genericText=new GenericConstructor<string>('text');
console.log(genericNumber.value+1,genericText.value.length);
function Captures(this:any,value:number):void {this.value=value;this.read=()=>this.value;}
const Capturing:any=Captures;const captured=new Capturing(31);const read=captured.read;
console.log(read.call({value:99}),captured instanceof Capturing);
