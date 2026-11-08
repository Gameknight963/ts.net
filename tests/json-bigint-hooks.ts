export {};
const prototype: any=BigInt.prototype;
try {console.log(JSON.stringify(1n));}catch(error){console.log('no hook',(error as Error).name);}
prototype.toJSON=function(this: bigint|BigInt,key:string){return key+':'+this.toString();};
console.log('root',JSON.stringify(123n));
console.log('nested',JSON.stringify({value:456n,array:[7n,8n]}));
console.log('boxed',JSON.stringify(Object(9n)));
const seen:string[]=[];
console.log('replacer',JSON.stringify({value:10n},function(key,value:unknown){seen.push(key+':'+typeof value);return value;}),seen.join(','));
prototype.toJSON=function(){return undefined;};
console.log('undefined',JSON.stringify(1n),JSON.stringify({value:1n}),JSON.stringify([1n]));
prototype.toJSON=function(){return null;};console.log('null',JSON.stringify(1n));
prototype.toJSON=function(){return 42;};console.log('number',JSON.stringify(1n));
prototype.toJSON=function(){return 2n;};
try {JSON.stringify(1n);}catch(error){console.log('bigint result',(error as Error).name);}
const marker=new Error('hook');prototype.toJSON=function(){throw marker;};
try {JSON.stringify(1n);}catch(error){console.log('throw',error===marker);}
prototype.toJSON=3;
try {JSON.stringify(1n);}catch(error){console.log('noncallable',(error as Error).name);}
delete prototype.toJSON;
try {JSON.stringify(1n);}catch(error){console.log('removed',(error as Error).name);}
