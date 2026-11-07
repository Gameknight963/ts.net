export {};
class Derived extends ArrayBuffer {marker=7;}
class Plain extends ArrayBuffer {static get [Symbol.species]():any{return null;}}
class Bigger extends ArrayBuffer {static get [Symbol.species]():any{return class {constructor(length:number){const result=new ArrayBuffer(length+2);new Uint8Array(result).fill(9);return result as any;}};}}
for(const Constructor of [Derived,Plain,Bigger]) {
 const source=new Constructor(4);new Uint8Array(source).set([1,2,3,4]);
 const result=source.slice(1,3);
 console.log(Constructor.name,result.constructor.name,result instanceof Derived,result.byteLength,Array.from(new Uint8Array(result)).join('|'));
}
console.log('getter',ArrayBuffer[Symbol.species]===ArrayBuffer,Derived[Symbol.species]===Derived);
for(const constructor of [undefined,null,1,{[Symbol.species]:null},{[Symbol.species]:undefined},{[Symbol.species]:()=>{}},{[Symbol.species]:Uint8Array},{[Symbol.species]:class {constructor(){return new ArrayBuffer(0) as any;}}}]) {
 const source:any=new ArrayBuffer(4);source.constructor=constructor;
 try{console.log('constructor',source.slice(1,3).byteLength);}catch(error){console.log('invalid',(error as Error).name);}
}
const same:any=new ArrayBuffer(4);same.constructor={[Symbol.species]:class {constructor(){return same;}}};
for(const length of [0,2])try{same.slice(0,length);}catch(error){console.log('same',(error as Error).name);}
let events='';const ordered:any=new ArrayBuffer(4);new Uint8Array(ordered).set([1,2,3,4]);
ordered.constructor={get [Symbol.species](){events+='P';return class {constructor(length:number){events+='C'+length;new Uint8Array(ordered)[1]=8;return new ArrayBuffer(length) as any;}};}};
const result=ordered.slice({valueOf(){events+='S';return 1;}},{valueOf(){events+='E';return 3;}});
console.log('order',events,Array.from(new Uint8Array(result)).join('|'));
const intrinsic=ArrayBuffer[Symbol.species];const descriptor=Object.getOwnPropertyDescriptor(ArrayBuffer,Symbol.species)!;
console.log('metadata',intrinsic===ArrayBuffer,descriptor.enumerable,descriptor.configurable,descriptor.set===undefined,descriptor.get!.name,descriptor.get!.length);
