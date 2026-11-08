export {};
const method=String.prototype[Symbol.iterator];
console.log('metadata',method.name,method.length);
const iterator=method.call('a\ud83d\ude00b');
console.log('self',iterator[Symbol.iterator]()===iterator,Object.prototype.toString.call(iterator));
for(let index=0;index<5;index++){const result=iterator.next();console.log('next',JSON.stringify(result.value),result.done);}
console.log('generic',Array.from(method.call(123 as any)).join(','));
for(const value of [null,undefined])try{method.call(value as any);}catch(error){console.log('invalid',(error as Error).name);}
const boxed:any=Object('original');boxed.toString=()=> 'changed';
console.log('boxed',Array.from(boxed).join(','),Array.from(method.call(boxed)).join(','));
const prototype:any=String.prototype;
try {
 prototype[Symbol.iterator]=function*(this:unknown){console.log('receiver',this==='text');yield 'override';};
 console.log('from',Array.from('text').join(','));
 const values:string[]=[];for(const value of 'text')values.push(value);console.log('loop',values.join(','));
 console.log('spread',[...'text'].join(','));
 prototype[Symbol.iterator]=null;
 try{for(const value of 'text')console.log(value);}catch(error){console.log('disabled',(error as Error).name);}
 console.log('array-like',JSON.stringify(Array.from('a\ud83d\ude00b')));
 delete prototype[Symbol.iterator];
 try{for(const value of 'text')console.log(value);}catch(error){console.log('deleted',(error as Error).name);}
}finally{prototype[Symbol.iterator]=method;}
console.log('restored',[...'text'].join(','));

class Swapping extends Array<string> {constructor(){super();prototype[Symbol.iterator]=function*(){yield 'changed';};}}
const from:any=Array.from;
try{const result:any=from.call(Swapping,'abc');console.log('captured',result.join(','));}finally{prototype[Symbol.iterator]=method;}
