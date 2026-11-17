export {};
let effects=0;
function hit():void {effects++;}
class Overwritten {value:number|undefined=void hit();constructor(value:number){this.value=value;}}
function escape():Overwritten{return new Overwritten(3);}
const escaped=escape();console.log("escaped",escaped.value,effects);
function local():number {const value=new Overwritten(5);return value.value!;}
console.log("local",local(),effects);
const original=Array.prototype.push;let changed=0;
function replacePush():void {Array.prototype.push=function(...values:any[]):number{changed++;return Reflect.apply(original,this,values);};}
class ChangesPush {value:number|undefined=void replacePush();}
function fill():ChangesPush[]{const values:ChangesPush[]=[];for(let i=0;i<3;i++)values.push(new ChangesPush());return values;}
let count=0;try{count=fill().length;}finally{Array.prototype.push=original;}
console.log("push",count,changed);
const trace:string[]=[];
function observe(value:any):void {trace.push(Object.hasOwn(value,"value")+":"+Object.hasOwn(value,"other"));}
class Observed {value:number|undefined=void observe(this);other=7;constructor(value:number){this.value=value;}}
const observed=new Observed(9);console.log("order",observed.value,observed.other,trace.join(","));
function lock(value:any):void {Object.defineProperty(value,"value",{value:1,writable:false,configurable:false});}
class Locked {value:number|undefined=void lock(this);}
try{new Locked();}catch(error){console.log("locked",(error as Error).name);}
