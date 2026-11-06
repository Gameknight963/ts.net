const own={present:undefined,value:3};
console.log('present' in own,'missing' in own,'value' in own,Object.hasOwn(own,'present'));
const optional:{value?:number}={};console.log('value' in optional);optional.value=undefined;console.log('value' in optional);
let getterReads=0;const accessor={get value(){getterReads++;return 1;}};
console.log('value' in accessor,getterReads);
for(const key of ['constructor','toString','valueOf','hasOwnProperty','__proto__'])console.log(key,key in own,Object.hasOwn(own,key));
const sparse:any[]=[,undefined,3];sparse.length=5;
console.log(0 in sparse,1 in sparse,2 in sparse,3 in sparse,4 in sparse,-0 in sparse,'length' in sparse,'push' in sparse,'at' in sparse,'toString' in sparse);
(sparse as any)['01']=undefined;(sparse as any)[4294967295]=1;
console.log('01' in sparse,4294967295 in sparse,4294967294 in sparse,NaN in sparse,Infinity in sparse);
const numeric=[1,2];numeric.length=4;console.log(0 in numeric,2 in numeric);numeric[2]=undefined as any;console.log(2 in numeric);
class Base {field=1;method():void {}get inherited():number {getterReads++;return 7;}static shared=2;}
class Derived extends Base {own=3;}
const instance=new Derived();
console.log('field' in instance,'own' in instance,'method' in instance,'inherited' in instance,'constructor' in instance,'toString' in instance,getterReads);
console.log('shared' in Derived,'prototype' in Derived,'call' in Derived,'name' in Derived,'length' in Derived,'toString' in Derived);
class NullBase extends null {}
console.log('constructor' in NullBase.prototype,'toString' in NullBase.prototype,'__proto__' in NullBase.prototype);
const callback=(value:number)=>value;
console.log('length' in callback,'name' in callback,'call' in callback,'apply' in callback,'bind' in callback,'toString' in callback);
const error=new Error();console.log('name' in error,'message' in error,'stack' in error,'cause' in error);
let effects='';
const keyObject={toString(){effects+='K';return 'value';}};
function left():any{effects+='L';return keyObject;}
function right():object{effects+='R';return own;}
console.log(left() in right(),effects);
effects='';try{console.log(left() in (0 as any));}catch(error){console.log(error instanceof TypeError,effects);}
for(const target of [undefined,null,0,false,'abc'] as any[]){try{console.log('x' in target);}catch(error){console.log(error instanceof TypeError);}}
const badKey={toString(){return {};},valueOf(){return {};}};
try{console.log((badKey as any) in own);}catch(error){console.log(error instanceof TypeError);}
const mutationTarget:any={};const mutatingKey={toString(){mutationTarget.added=undefined;return 'added';}};
console.log((mutatingKey as any) in mutationTarget);
const primitiveNames:any={undefined:1,null:2,true:3,NaN:4,Infinity:5};
console.log((undefined as any) in primitiveNames,(null as any) in primitiveNames,(true as any) in primitiveNames,NaN in primitiveNames,Infinity in primitiveNames);
type Choice={x:number}|{y:string};
function narrow(value:Choice):string {return 'x' in value?'x'+value.x:'y'+value.y;}
console.log(narrow({x:4}),narrow({y:'yes'}));
enum NullPrototype {__proto__=null as any}
console.log('toString' in NullPrototype,'__proto__' in NullPrototype,'null' in NullPrototype);
function guarded(items:number[],key:any):number {let total=0;for(let i=0;i<items.length;i++){if(key in {})total++;total+=items[i]??0;}return total;}
const shrinking=[1,2,3];console.log(guarded(shrinking,{toString(){shrinking.pop();return 'absent';}}),shrinking.length);
function ordinary():void {}
const expression=function named():void {};
const methods={method():void {}};
const ordinaryValue:any=ordinary;
console.log('prototype' in ordinary,'prototype' in expression,'prototype' in callback,'prototype' in methods.method,'prototype' in ordinary.bind(null));
console.log(ordinaryValue.prototype.constructor===ordinary,ordinaryValue.prototype===ordinaryValue.prototype,Object.keys(ordinaryValue.prototype).length);
ordinaryValue.prototype.extra=1;console.log('extra' in ordinaryValue.prototype,Object.keys(ordinaryValue.prototype).length);
ordinaryValue.prototype=null;console.log('prototype' in ordinary,ordinaryValue.prototype===null);

console.log('prototype' in Object,'prototype' in Number,'prototype' in Error);

for(const method of ['map','join','findLast','flatMap','reduce','values','toReversed','with','isArray','prototype','missingMethod'])console.log(method,method in sparse);
