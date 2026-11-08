export {};
const method=Date.prototype[Symbol.toPrimitive];
console.log('metadata',method.name,method.length);
let events='';const object:any={toString(){events+='string|';return 'text';},valueOf(){events+='number|';return 42;}};
for(const hint of ['default','string','number']){events='';console.log('generic',hint,method.call(object,hint),events);}
for(const hint of [undefined,null,1,'invalid',{}])try{method.call(object,hint as any);}catch(error){console.log('hint',(error as Error).name);}
for(const receiver of [null,undefined,1,'date'])try{method.call(receiver as any,'number');}catch(error){console.log('receiver',(error as Error).name);}
const fallback:any={toString(){return {};},valueOf(){return 7;}};console.log('fallback',method.call(fallback,'default'));
const date=new Date(0);console.log('date',method.call(date,'number'),method.call(date,'string')===date.toString(),String(date)===date.toString(),+date);
const invalid=new Date(NaN);console.log('invalid',method.call(invalid,'string'),Number.isNaN(method.call(invalid,'number') as number));
const prototype:any=Date.prototype,descriptor=Object.getOwnPropertyDescriptor(prototype,Symbol.toPrimitive)!;
console.log('descriptor',descriptor.writable,descriptor.enumerable,descriptor.configurable);
try{prototype[Symbol.toPrimitive]=()=>1;}catch(error){console.log('readonly',(error as Error).name);}
try{delete prototype[Symbol.toPrimitive];console.log('ordinary default',(date as any)+1);prototype[Symbol.toPrimitive]=method;console.log('recreated',Object.getOwnPropertyDescriptor(prototype,Symbol.toPrimitive)!.enumerable);}
finally{prototype[Symbol.toPrimitive]=method;}
