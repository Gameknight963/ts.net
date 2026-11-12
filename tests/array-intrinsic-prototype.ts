export {};
const prototype=Array.prototype;
console.log('same',Object.getPrototypeOf([])===prototype,Object.getPrototypeOf([1,2])===prototype);
console.log('same definition',Reflect.defineProperty(Array,'prototype',{value:prototype}));
console.log('replacement',Reflect.defineProperty(Array,'prototype',{value:{}}));
console.log('accessor replacement',Reflect.defineProperty(Array,'prototype',{get(){return {};}}));
const originalPush=prototype.push;
let writes=0;
try {
 const target:number[]=[];
 function argument():number {Object.defineProperty(prototype,'0',{get(){return 23;},set(value:number){writes+=value;},configurable:true});return 5;}
 target.push(argument());
 console.log('argument mutation',target.length,Object.hasOwn(target,0),target[0],writes);
} finally {delete (prototype as any)[0];prototype.length=0;}
let reads=0;
try {
 Object.defineProperty(prototype,'push',{get(){reads++;return originalPush;},configurable:true});
 const values:number[]=[];values.push(7);values.push(11);
 console.log('push getter',reads,values.length,values[0],values[1]);
} finally {Object.defineProperty(prototype,'push',{value:originalPush,writable:true,configurable:true});}
const custom:any=Object.create(prototype);custom.push=originalPush;
const value:number[]=[];Object.setPrototypeOf(value,custom);value.push(13);console.log('custom prototype',value[0],value.length);
const nullPrototype:number[]=[];Object.setPrototypeOf(nullPrototype,null);originalPush.call(nullPrototype,17);console.log('null prototype',nullPrototype[0],nullPrototype.length);
