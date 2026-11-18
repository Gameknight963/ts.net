class Point { value=0; name="item"; enabled=true; constructor(value:number){this.value=value;} }
function build(count:number) { const values:Point[]=[];for(let i=0;i<count;i++)values.push(new Point(i));return values; }
console.log(build(4).map(p=>p.value+p.name.length+(p.enabled?1:0)).join(","));
const original = Array.prototype.push;
let changedCalls=0;
class Changing { value=0; constructor(value:number){this.value=value;Array.prototype.push=function(...items:any[]):number{changedCalls++;return Reflect.apply(original,this,items);};} }
function changing(count:number){const values:Changing[]=[];for(let i=0;i<count;i++)values.push(new Changing(i));return values;}
let changed:Changing[];
try { changed=changing(4); } finally { Array.prototype.push=original; }
console.log("changed",changedCalls,changed!.map(p=>p.value).join(","));
const descriptor=Object.getOwnPropertyDescriptor(Array.prototype,"push")!;
let reads=0;
try {Object.defineProperty(Array.prototype,"push",{configurable:true,get(){reads++;return original;}});build(4);}
finally {Object.defineProperty(Array.prototype,"push",descriptor);}
console.log("getter",reads);
console.log("empty",build(0).length);
let setterCalls=0;
class Undeclared { constructor(value:number){(this as any).value=value;} }
Object.defineProperty(Undeclared.prototype,"value",{configurable:true,set(_value:number){setterCalls++;Array.prototype.push=function(...items:any[]):number{changedCalls++;return Reflect.apply(original,this,items);};}});
function undeclared(count:number){const values:Undeclared[]=[];for(let i=0;i<count;i++)values.push(new Undeclared(i));return values;}
let undeclaredCount=0;changedCalls=0;
try {undeclaredCount=undeclared(3).length;} finally {Array.prototype.push=original;}
console.log("setter",setterCalls,changedCalls,undeclaredCount);
function beforeDeclaration(count:number){const values:Late[]=[];for(let i=0;i<count;i++)values.push(new Late(i));return values;}
console.log("before empty",beforeDeclaration(0).length);
try {beforeDeclaration(1);} catch {console.log("before rejected");}
class Late {value=0;constructor(value:number){this.value=value;}}
console.log("after",beforeDeclaration(2).map(p=>p.value).join(","));
function large(count:number) {const values:Large[]=[];for(let i=0;i<count;i++)values.push(new Large());return values;}
console.log('large before empty', large(0).length);
try {large(1);} catch(error) {console.log('large before', (error as Error).name);}
class Large {
 f0=0;f1=1;f2=2;f3=3;f4=4;f5=5;f6=6;f7=7;
 f8=8;f9=9;f10=10;f11=11;f12=12;f13=13;f14=14;f15=15;f16=16;
}
console.log('large values', large(4).map(value=>value.f0+value.f16).join(','));
reads=0;
try {Object.defineProperty(Array.prototype,'push',{configurable:true,get(){reads++;return original;}});large(4);}
finally {Object.defineProperty(Array.prototype,'push',descriptor);}
console.log('large getter', reads);
class LargeParameter {
 f0=0;f1=1;f2=2;f3=3;f4=4;f5=5;f6=6;f7=7;
 f8=8;f9=9;f10=10;f11=11;f12=12;f13=13;f14=14;f15=15;f16=16;
 constructor(value:number){this.f16=value;}
}
function largeParameters(count:number) {const values:LargeParameter[]=[];for(let i=0;i<count;i++)values.push(new LargeParameter(i));return values;}
console.log('large parameters', largeParameters(4).map(value=>value.f16).join(','));
function changingArgument(value:number):number {
 Array.prototype.push=function(...items:any[]):number{changedCalls++;return Reflect.apply(original,this,items);};
 return value;
}
function largeArguments(count:number) {const values:LargeParameter[]=[];for(let i=0;i<count;i++)values.push(new LargeParameter(changingArgument(i)));return values;}
changedCalls=0;
let argumentValues:LargeParameter[]=[];
try {argumentValues=largeArguments(4);} finally {Array.prototype.push=original;}
console.log('large argument effects', changedCalls, argumentValues.map(value=>value.f16).join(','));
