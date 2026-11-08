export {};
function inspect(..._values: unknown[]):void {
 const value:any=arguments;
 console.log('identity',value[Symbol.iterator]===Array.prototype.values,Object.hasOwn(value,Symbol.iterator));
 const descriptor=Object.getOwnPropertyDescriptor(value,Symbol.iterator)!;
 console.log('descriptor',descriptor.enumerable,descriptor.writable,descriptor.configurable);
 console.log('symbols',Object.getOwnPropertySymbols(value).includes(Symbol.iterator),Object.keys(value).join(','));
 console.log('spread property',Object.hasOwn({...value},Symbol.iterator));
 const extra=Symbol('extra');value[extra]=true;value.extra='extra';
 console.log('symbol order',Object.getOwnPropertySymbols(value).map(symbol=>symbol===Symbol.iterator?'iterator':'extra').join(','));
 console.log('names',Object.getOwnPropertyNames(value).join(','));
 console.log('explicit',Array.from(value[Symbol.iterator]()).join(','));
 value[Symbol.iterator]=function*(){yield 'changed';};
 console.log('custom',Array.from(value).join(','),Object.getOwnPropertyDescriptor(value,Symbol.iterator)!.enumerable);
 value[Symbol.iterator]=null;
 try{for(const item of value)console.log(item);}catch(error){console.log('disabled',(error as Error).name);}
 console.log('array-like',Array.from(value).join(','));
 delete value[Symbol.iterator];
 try{for(const item of value)console.log(item);}catch(error){console.log('deleted',(error as Error).name);}
 value[Symbol.iterator]=Array.prototype.values;
 console.log('recreated order',Object.getOwnPropertySymbols(value).map(symbol=>symbol===Symbol.iterator?'iterator':'extra').join(','));
 console.log('recreated',Object.getOwnPropertyDescriptor(value,Symbol.iterator)!.enumerable,Object.hasOwn({...value},Symbol.iterator),Array.from(value).join(','));
}
inspect(1,2,3);
