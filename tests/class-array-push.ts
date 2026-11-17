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
