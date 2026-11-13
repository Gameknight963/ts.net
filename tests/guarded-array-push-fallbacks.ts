export {};
function configured(count:number,setup:(source:any,copies:any[])=>void):number {
 const source={x:0,y:2};
 const copies:{x:number;y:number}[]=[];
 setup(source,copies);
 for(let i=0;i<count;i++) {
  source.x=i+7;
  const copy={...source};
  copy.y=i;
  copies.push(copy);
 }
 let total=0;for(const copy of copies)total+=copy.x+copy.y;return total;
}
console.log('configured',configured(3,()=>{}));
let calls=0;
console.log('own push',configured(3,(_,array)=>{array.push=function(this:any,value:any){calls++;return Array.prototype.push.call(this,value);};}),calls);
let reads=0;
console.log('own getter',configured(3,(_,array)=>{Object.defineProperty(array,'push',{get(){reads++;return Array.prototype.push;},configurable:true});}),reads);
let exposed:any;
try {configured(3,(source,array)=>{exposed=source;Object.defineProperty(array,'length',{writable:false});});}catch{console.log('readonly',exposed.x===7);}
try {configured(3,(source,array)=>{exposed=source;Object.preventExtensions(array);});}catch{console.log('nonextensible',exposed.x===7);}
console.log('nonempty',configured(3,(_,array)=>{array.push({x:3,y:4});}));
const prototype=Array.prototype;const originalPush=prototype.push;
let intercepted:any;let writes=0;
try {
 console.log('prototype index',configured(3,()=>{Object.defineProperty(prototype,'0',{get(){return intercepted;},set(value:any){writes++;intercepted=value;},configurable:true});}),writes);
}finally{delete (prototype as any)[0];prototype.length=0;}
let prototypeReads=0;
try {
 console.log('prototype getter',configured(3,(source)=>{exposed=source;Object.defineProperty(prototype,'push',{get(){prototypeReads++;source.x+=100;return originalPush;},configurable:true});}),prototypeReads,exposed.x);
}finally{Object.defineProperty(prototype,'push',{value:originalPush,writable:true,configurable:true});}
let effects=0;
function effect(value:number):number{effects++;return value;}
function callsInBody():number {
 const source={x:0,y:2};const copies:{x:number;y:number}[]=[];
 for(let i=0;i<3;i++){source.x=effect(i+7);const copy={...source};copy.y=i;copies.push(copy);}
 let total=0;for(const copy of copies)total+=copy.x+copy.y;return total;
}
console.log('calls in body',callsInBody(),effects);
let getterReads=0;let setterWrites=0;let stored=0;
console.log('source accessor',configured(3,(source)=>{Object.defineProperty(source,'x',{get(){getterReads++;return stored;},set(value:number){setterWrites++;stored=value;},enumerable:true,configurable:true});}),getterReads,setterWrites);
try {configured(3,(source)=>{exposed=source;Object.freeze(source);});}catch{console.log('frozen source',exposed.x===0);}
console.log('custom array prototype',configured(3,(_,array)=>{Object.setPrototypeOf(array,Object.create(Array.prototype));}));
try {configured(3,(source,array)=>{exposed=source;Object.setPrototypeOf(array,null);});}catch{console.log('null array prototype',exposed.x===7);}
let inheritedReads=0;
try {
 delete (prototype as any).push;
 Object.defineProperty(Object.prototype,'push',{get(){inheritedReads++;return originalPush;},configurable:true});
 console.log('inherited push getter',configured(3,()=>{}),inheritedReads);
}finally{delete (Object.prototype as any).push;Object.defineProperty(prototype,'push',{value:originalPush,writable:true,configurable:true});}
