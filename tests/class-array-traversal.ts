class Point {x=0;y=0;constructor(x:number,y:number){this.x=x;this.y=y;}}
function sum(items:Point[]):number {let total=0;for(let i=0;i<items.length;i++)total+=items[i].x+items[i].y;return total;}
const points:Point[]=[];for(let i=0;i<8;i++)points.push(new Point(i,i+1));
console.log("plain",sum(points),sum([]));points[0].x=20;console.log("changed",sum(points));
console.log("nonfinite",sum([new Point(NaN,1)]),sum([new Point(Infinity,2)]));
const absent=new Point(1,2);(absent as any).x=undefined;console.log("undefined",sum([absent]));
const deleted=new Point(3,4);delete (deleted as any).x;console.log("deleted",sum([deleted]));
Object.defineProperty(Point.prototype,"x",{get(){return 7;},configurable:true});console.log("inherited",sum([deleted]));delete (Point.prototype as any).x;
let gets=0;const changing:Point[]=[new Point(1,2),new Point(3,4)];
Object.defineProperty(changing[0],"x",{get(){gets++;changing[0]=new Point(10,20);changing.push(new Point(30,40));return 1;},configurable:true});
console.log("getter changes array",sum(changing),gets,changing.length);
const indexed:Point[]=[new Point(1,2)];Object.defineProperty(indexed,"0",{get(){gets++;return new Point(gets,gets);}});console.log("index getter",sum(indexed),gets);
const sparse:Point[]=new Array(2);sparse[1]=new Point(5,6);const prototype=Object.create(Array.prototype);Object.defineProperty(prototype,"0",{get(){gets++;return new Point(gets,7);}});Object.setPrototypeOf(sparse,prototype);console.log("sparse",sum(sparse),gets);
const fixed=[new Point(8,9)];Object.freeze(fixed[0]);Object.freeze(fixed);console.log("frozen",sum(fixed));
const arrayOnly=[new Point(8,9)];Object.freeze(arrayOnly);console.log("frozen array",sum(arrayOnly));
const definedIndex=[new Point(1,2)];Object.defineProperty(definedIndex,"0",{value:new Point(20,21),writable:false,enumerable:false,configurable:false});console.log("defined index",sum(definedIndex));
const noPrototype=new Point(10,11);Object.setPrototypeOf(noPrototype,null);console.log("null prototype",sum([noPrototype]));
const record:Point={x:12,y:13};console.log("structural",sum([record]),sum([new Point(14,15),record]));
let traps=0;const proxy=new Proxy(new Point(16,17),{get(target,key,receiver){traps++;return Reflect.get(target,key,receiver);}});console.log("proxy item",sum([proxy]),traps);
traps=0;const arrayProxy=new Proxy([new Point(18,19)],{get(target,key,receiver){traps++;return Reflect.get(target,key,receiver);}});console.log("proxy array",sum(arrayProxy),traps);
function shifted(items:Point[]):number {let total=0;for(let i=3;i<items.length;i++)total+=items[i].x-items[i].y*2+i;return total;}
console.log("shifted",shifted(points),shifted([]));
const earlier:Point[]=[null as any,new Point(1,2),new Point(3,4)];console.log("empty shifted",shifted(earlier));
class Wide {a=0;b=1;c=2;d=3;e=4;f=5;g=6;h=7;i=8;j=9;k=10;l=11;m=12;n=13;o=14;p=15;}
function wide(items:Wide[]):number {let total=0;for(let i=0;i<items.length;i++)total+=items[i].a+items[i].p;return total;}
console.log("wide mask",wide([new Wide(),new Wide()]));const missingWide=new Wide();(missingWide as any).p=undefined;console.log("wide undefined",wide([missingWide]));
try{sum([null as any]);}catch(error){console.log("null",(error as Error).name);}
let uninitialized:Point[];try{sum(uninitialized!);}catch(error){console.log("uninitialized",(error as Error).name);}

class Item {value=0;name='item';enabled=true;constructor(value:number){this.value=value;}}
function mixed(items:Item[]):number {
 let total=0;
 for(let i=0;i<items.length;i++)total+=items[i].value+items[i].name.length+(items[i].enabled?1:0);
 return total;
}
function names(items:Item[]):number {
 let total=0;
 for(let i=0;i<items.length;i++)total+=items[i].enabled?items[i].name.length:items[i].value;
 return total;
}
const mixedItems=[new Item(1),new Item(2)];
console.log('mixed',mixed(mixedItems),names(mixedItems),mixed([]));
mixedItems[0].name='longer';mixedItems[1].enabled=false;
console.log('mixed writes',mixed(mixedItems),names(mixedItems));
const missingFlag=new Item(3);(missingFlag as any).enabled=undefined;
console.log('missing flag',mixed([missingFlag]),names([missingFlag]));
const deletedName=new Item(4);delete (deletedName as any).name;
Object.defineProperty(Item.prototype,'name',{get(){return 'inherited';},configurable:true});
console.log('inherited name',mixed([deletedName]));delete (Item.prototype as any).name;
let mixedGets=0;const changingMixed=[new Item(1),new Item(2)];
Object.defineProperty(changingMixed[0],'name',{get(){
 mixedGets++;changingMixed[0]=new Item(10);changingMixed.push(new Item(20));return 'old';
},configurable:true});
console.log('name getter changes array',mixed(changingMixed),mixedGets,changingMixed.length);
const flagItems=[new Item(1)];
Object.defineProperty(flagItems[0],'enabled',{get(){mixedGets++;flagItems.push(new Item(3));return false;},configurable:true});
console.log('flag getter changes array',mixed(flagItems),mixedGets,flagItems.length);
let mixedTraps=0;
const mixedProxy=new Proxy(new Item(5),{get(target,key,receiver){mixedTraps++;return Reflect.get(target,key,receiver);}});
console.log('mixed proxy',mixed([mixedProxy]),mixedTraps);
const mixedRecord:Item={value:6,name:'structural',enabled:false};
console.log('mixed structural',mixed([new Item(7),mixedRecord]));
const frozenMixed=new Item(8);Object.freeze(frozenMixed);
console.log('mixed frozen',mixed(Object.freeze([frozenMixed]) as unknown as Item[]));
const nullName=new Item(9);(nullName as any).name=null;
try{mixed([nullName]);}catch{console.log('null name throws');}
const undefinedName=new Item(9);(undefinedName as any).name=undefined;
try{mixed([undefinedName]);}catch{console.log('undefined name throws');}
