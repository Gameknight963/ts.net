const first:symbol=Symbol("x"),second:symbol=Symbol("x");console.log("identity",first===first,first===second,typeof first,Boolean(first));
for(const value of [Symbol(),Symbol(undefined),Symbol(""),Symbol("hello"),Symbol(123 as any)])console.log("text",String(value),value.toString(),value.description);
const global=Symbol.for("shared");console.log("registry",global===Symbol.for("shared"),Symbol.keyFor(global),Symbol.keyFor(first));console.log("undefined key",Symbol.keyFor(Symbol.for(undefined as any)));
const boxed=Object(first);console.log("box",typeof boxed,boxed.valueOf()===first,Object.prototype.toString.call(boxed),Object.prototype.toString.call(first),(boxed as any)==first);
for(const operation of [()=>Number(first),()=>String(boxed),()=>new String(first),()=>""+(first as any),()=>+(first as any),()=>new (Symbol as any)(),()=>Symbol.keyFor(1 as any)])try{operation();}catch(error){console.log("conversion",(error as Error).name);}
console.log("json",JSON.stringify([first]),JSON.stringify({value:first}),JSON.stringify(boxed),JSON.stringify(first));
try{Symbol.prototype.toString.call({});}catch(error){console.log("receiver",(error as Error).name);}
console.log("metadata",Symbol.name,Symbol.length,Symbol.for.length,Symbol.keyFor.length);
console.log("well known",typeof Symbol.iterator,Symbol.iterator===Symbol.iterator,Symbol.keyFor(Symbol.iterator),Symbol.iterator.description);
const descriptor=Object.getOwnPropertyDescriptor(Symbol,"iterator")!;console.log("descriptor",descriptor.writable,descriptor.enumerable,descriptor.configurable);
try{(Symbol as any).iterator=Symbol();}catch(error){console.log("readonly",(error as Error).name);}
