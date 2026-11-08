export {};
const symbol=Symbol('value'),boxed=Object(symbol),method=Symbol.prototype[Symbol.toPrimitive];
console.log('metadata',method.name,method.length);
for(const receiver of [symbol,boxed])for(const hint of ['default','string','number',undefined,null,{}])console.log('convert',method.call(receiver,hint as any)===symbol);
console.log('boxed',Symbol.prototype.valueOf.call(boxed)===symbol,Symbol.prototype.toString.call(boxed),boxed.description);
const object:any={};object[boxed]=42;console.log('key',object[symbol],Object.getOwnPropertySymbols(object)[0]===symbol);
for(const receiver of [null,undefined,{},'value',1,Symbol.prototype])try{method.call(receiver as any,'string');}catch(error){console.log('receiver',(error as Error).name);}
for(const key of [Symbol.toPrimitive,Symbol.toStringTag]) {
 const descriptor=Object.getOwnPropertyDescriptor(Symbol.prototype,key)!;
 console.log('descriptor',descriptor.writable,descriptor.enumerable,descriptor.configurable);
 try{(Symbol.prototype as any)[key]='changed';}catch(error){console.log('readonly',(error as Error).name);}
}
const prototype:any=Symbol.prototype;
const originalValueOf=prototype.valueOf;
try{
 prototype.valueOf=function(){return 42;};
 try{console.log(Number(boxed));}catch(error){console.log('exotic preferred',(error as Error).name);}
 delete prototype[Symbol.toPrimitive];console.log('deleted',Object.hasOwn(prototype,Symbol.toPrimitive));
 console.log('ordinary',String(boxed),Number(boxed));
 prototype[Symbol.toPrimitive]=method;console.log('recreated',Object.getOwnPropertyDescriptor(prototype,Symbol.toPrimitive)!.enumerable);
}
finally{prototype[Symbol.toPrimitive]=method;prototype.valueOf=originalValueOf;}
