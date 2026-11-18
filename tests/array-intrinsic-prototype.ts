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

const plain: object[] = [];
const key = 'arrayReadProbe';
try {
    (prototype as any)[key] = 'first';
    console.log('prototype data', (plain as any)[key]);
    (prototype as any)[key] = 'second';
    console.log('changed data', (plain as any)[key]);
    (prototype as any)[key] = null;
    console.log('null data', (plain as any)[key] === null);
    (prototype as any)[key] = undefined;
    console.log('undefined data', (plain as any)[key] === undefined);
    Object.defineProperty(prototype, key, {configurable: true, get() {return this === plain;}});
    console.log('getter receiver', (plain as any)[key]);
    Object.defineProperty(plain, key, {configurable: true, value: 'own'});
    console.log('own data', (plain as any)[key]);
    delete (plain as any)[key];
    console.log('deleted own data', (plain as any)[key]);
    Object.setPrototypeOf(plain, {[key]: 'custom'});
    console.log('changed prototype', (plain as any)[key]);
} finally {delete (prototype as any)[key];}
