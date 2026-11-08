export {};
const method=Array.prototype[Symbol.iterator];
console.log('identity',method===Array.prototype.values,method.name,method.length);
const values=[1,,3],iterator=method.call(values);
console.log('tag',Object.prototype.toString.call(iterator),iterator[Symbol.iterator]()===iterator);
console.log('first',JSON.stringify(iterator.next()));values.push(4);
for(let index=0;index<4;index++)console.log('next',JSON.stringify(iterator.next()));
const prototype:any=Array.prototype;
try {
 prototype[Symbol.iterator]=function*(){yield 7;yield 8;};
 console.log('override',Array.from([1,2,3]).join(','));
 const collected:number[]=[];for(const value of [1,2,3])collected.push(value);console.log('loop',collected.join(','));
 console.log('spread',[...[1,2,3]].join(','));
 prototype[Symbol.iterator]=null;
 try{for(const value of [1,2])console.log(value);}catch(error){console.log('disabled',(error as Error).name);}
 console.log('array-like',Array.from([1,2,3]).join(','));
 delete prototype[Symbol.iterator];
 try{for(const value of [1,2])console.log(value);}catch(error){console.log('deleted',(error as Error).name);}
}finally{prototype[Symbol.iterator]=method;}
console.log('restored',[...[1,2,3]].join(','));
for(const source of [[1,2],'ab']) {
 const proto:any=Object.getPrototypeOf((source as any)[Symbol.iterator]()),next=proto.next;let calls=0;
 try{proto.next=function(){return ++calls===1?{value:'custom',done:false}:{done:true};};console.log('next override',Array.from(source as any).join(','),calls);}
 finally{proto.next=next;}
}
