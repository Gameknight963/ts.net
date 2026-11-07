const setPrototypeOf=Reflect.setPrototypeOf;
const newPrototype={inherited:7};
for(const value of [{own:1},[],()=>1,Object('text'),Object(3),new Map(),new Set(),new WeakMap(),new Error('x'),Object.create(null)]) {
    console.log('set',setPrototypeOf(value,newPrototype),Reflect.getPrototypeOf(value)===newPrototype,(value as any).inherited);
    console.log('same',setPrototypeOf(value,newPrototype));
    console.log('null',setPrototypeOf(value,null),Reflect.getPrototypeOf(value));
}
const first:any={},second:any={},third:any={};
console.log('chain',setPrototypeOf(first,second),setPrototypeOf(second,third),setPrototypeOf(third,first),setPrototypeOf(first,first));
console.log('unchanged',Reflect.getPrototypeOf(first)===second,Reflect.getPrototypeOf(second)===third,Reflect.getPrototypeOf(third)===Object.prototype);
console.log('root',setPrototypeOf(Object.prototype,null),setPrototypeOf(Object.prototype,{}),Reflect.getPrototypeOf(Object.prototype));
const raw=(JSON as any).rawJSON('1');console.log('raw',setPrototypeOf(raw,null),setPrototypeOf(raw,newPrototype),Reflect.getPrototypeOf(raw));
for(const target of [null,undefined,3,'text',true])try{setPrototypeOf(target as any,newPrototype);}catch(error){console.log('invalid target',(error as Error).name);}
for(const prototype of [undefined,3,'text',true])try{setPrototypeOf({},prototype as any);}catch(error){console.log('invalid prototype',(error as Error).name);}
let conversions=0;const plain={toString():string{conversions++;return 'prototype';}};console.log('no conversion',setPrototypeOf({},plain),conversions);
const object={};Object.setPrototypeOf(object,newPrototype);console.log('object API',Object.getPrototypeOf(object)===newPrototype);
try{Object.setPrototypeOf(object,object);}catch(error){console.log('object cycle still throws',(error as Error).name);}
const descriptor=Object.getOwnPropertyDescriptor(Reflect,'setPrototypeOf')!;console.log('metadata',descriptor.value.name,descriptor.value.length,descriptor.writable,descriptor.enumerable,descriptor.configurable);

