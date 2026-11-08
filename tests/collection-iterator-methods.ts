export {};
console.log('identity',Set.prototype[Symbol.iterator]===Set.prototype.values,Set.prototype.keys===Set.prototype.values,Map.prototype[Symbol.iterator]===Map.prototype.entries);
const map=new Map([[1,'one'],[2,'two']]),set=new Set([3,4]);
console.log('map',JSON.stringify(Array.from(map[Symbol.iterator]())));
console.log('set',Array.from(set[Symbol.iterator]()).join(','));
console.log('tags',Object.prototype.toString.call(map.entries()),Object.prototype.toString.call(map.keys()),Object.prototype.toString.call(set.values()),Object.prototype.toString.call(set.entries()));
for(const value of [map,set]) {
 const prototype:any=Object.getPrototypeOf(value),method=prototype[Symbol.iterator];
 try {
  prototype[Symbol.iterator]=function*(){yield 'changed';};
  console.log('override',Array.from(value as any).join(','));
  const result:unknown[]=[];for(const item of value)result.push(item);console.log('loop',result.join(','));
  prototype[Symbol.iterator]=null;
  try{for(const item of value)console.log(item);}catch(error){console.log('disabled',(error as Error).name);}
  console.log('array-like',Array.from(value as any).length);
  delete prototype[Symbol.iterator];
  try{for(const item of value)console.log(item);}catch(error){console.log('deleted',(error as Error).name);}
 }finally{prototype[Symbol.iterator]=method;}
}
const iterator=set[Symbol.iterator]();console.log('self',iterator[Symbol.iterator]()===iterator,JSON.stringify(iterator.next()));set.add(5);console.log('growth',Array.from(iterator).join(','));
