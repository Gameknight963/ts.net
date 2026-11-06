console.log('empty', Array().length, new Array().length);
const holes = Array(3);
console.log('length', holes.length, Object.keys(holes).length, 0 in holes, holes[0]);
console.log('single', Array('3').length, Array('3')[0], Array(undefined).length, Array(null)[0]);
console.log('values', Array(1,2,3).join(','), new Array(1,2,3).join(','));
console.log('typed', new Array<number>(2).map(v=>v+1).length, Array.of(3).join(','), Array.of(1,2,3).join(','));
for(const length of [-1,1.5,NaN,Infinity,4294967296]) {try {Array(length);} catch(error) {console.log('range', length, error instanceof RangeError);}}
console.log('large', Array(4294967295).length, Array(-0).length);
console.log('brands', Array.isArray([]), Array.isArray(Array.prototype), Array.isArray({length:0}), Array.isArray('abc'), Array.isArray(null), Array.isArray(undefined));
console.log('instance', [] instanceof Array, holes instanceof Array, Array.of(3) instanceof Array);
console.log('prototype', Object.getPrototypeOf([])===Array.prototype, Object.getPrototypeOf(holes)===Array.prototype, Object.getPrototypeOf(Array.prototype)===Object.prototype, Array.prototype.constructor===Array);
console.log('hidden', Object.keys(Array).length, Object.keys(Array.prototype).length, Array.prototype.length);
const original = Array.prototype.map;
Array.prototype.map = function():any[] {return ['overridden'];};
console.log('override', [1,2].map(x=>x).join(','), Array(1,2).map(x=>x).join(','));
Array.prototype.map = original;
console.log('restore', [1,2].map(x=>x+1).join(','));
const of:any=Array.of;
console.log('of-extracted', of(4,5).join(','), of.call(null,6,7).join(','));
console.log('of-empty', Array.of().length);
class Values extends Array<number> {label='values'; total():number {return this.reduce((a,b)=>a+b,0);}}
const derived=new Values(1,2,3);
console.log('subclass', Array.isArray(derived), derived instanceof Values, derived instanceof Array, derived.length, derived.label, derived.total());
const made=Values.of(4,5);
console.log('subclass-of', Array.isArray(made), made instanceof Values, made.length, made.join(','));
function Container(this:any,length:number):void {this.requested=length;}
const container=of.call(Container,'a','b');
console.log('generic-of', container instanceof Container, container.requested, container.length, container[0], container[1]);
console.log('fallback-of', of.call(()=>({}),1,2).join(','));
console.log('metadata', Array.name, Array.length, Array.isArray.length, Array.of.length);
const spoof=Object.setPrototypeOf({},Array.prototype);
console.log('spoof', Array.isArray(spoof), spoof instanceof Array);
const detached: any[]=[];Object.setPrototypeOf(detached,null);
console.log('detached',Array.isArray(detached),detached instanceof Array,(detached as any).map);
const customPrototype:any={map():string {return 'custom';}};
const custom:any[]=[];Object.setPrototypeOf(custom,customPrototype);
console.log('custom-prototype',(custom as any).map());
const own:any[]=[1];own.map=function():any[] {return ['own'];};console.log('own-method',own.map(x=>x).join(','));
console.log('captured-prototype', derived.constructor===Values, Object.getPrototypeOf(derived)===Values.prototype);
let constructorEffects='';
function Recording(this:any,length:number):void {constructorEffects+='C'+length;this.length=99;}
const recorded=of.call(Recording,'x');console.log('of-constructor',constructorEffects,recorded.length,recorded[0]);
function Throws(this:any):void {throw 'constructor failure';}
try {of.call(Throws,1);} catch(error) {console.log('constructor-throw',error);}
const emptyObject=of.call(Object);console.log('generic-empty',Array.isArray(emptyObject),emptyObject.length);
function objectLength():void {console.log('should-not-coerce');}
const lengthObject={valueOf:objectLength};console.log('no-coercion',Array(lengthObject)[0]===lengthObject);
const methods:any=Array.prototype;
const push=methods.push,pop=methods.pop,slice=methods.slice,reverse=methods.reverse,indexOf=methods.indexOf,includes=methods.includes;
const overridden=[1,2,3];
methods.push=function(...items:any[]):number {return 90+items.length;};
console.log('push-override',overridden.push(4),overridden.push(...[4,5]),overridden.length);
methods.push=push;
methods.pop=()=>77;console.log('pop-override',overridden.pop(),overridden.length);methods.pop=pop;
methods.slice=()=>[8];console.log('slice-override',overridden.slice().join(','));methods.slice=slice;
methods.reverse=()=>[9];console.log('reverse-override',overridden.reverse().join(','),overridden.join(','));methods.reverse=reverse;
methods.indexOf=()=>42;methods.includes=()=>false;
console.log('search-override',overridden.indexOf(1),overridden.includes(1));methods.indexOf=indexOf;methods.includes=includes;
const capture=[1];
function replacePush():number {capture.push=()=>88;return 2;}
console.log('capture-method',capture.push(replacePush()),capture.length,capture[1],capture.push(3),capture.length);
const ownNative=[1,2];ownNative.reverse=reverse;console.log('own-native',ownNative.reverse().join(','));
let lookupEffects='';
const accessor:any[]=[];Object.setPrototypeOf(accessor,{get push():unknown {lookupEffects+='G';return function(this:any,value:number):number {lookupEffects+='C';return this.length+value;};}});
function argument():number {lookupEffects+='A';return 3;}
console.log('getter-call',accessor.push(argument()),lookupEffects);
let ignored=0;const popAny:any=[1];console.log('pop-extra',popAny.pop(++ignored),ignored);
const customObject:any={push(value:number):string{return 'custom'+value;}};console.log('custom-object',customObject.push(7));
const prototype:any=Array.prototype;
const saved=prototype.push;
prototype.push=function(this:any,value:any):number {this[0]='text';return 1;};
function inferred():void {const values:any[]=[];values.push(1);console.log('inferred-any',values[0]);}
inferred();prototype.push=saved;
