/// <reference lib="esnext.iterator" />
async function run():Promise<void>{
 for(const method of [null,undefined]){
  const array=[1,2];(array as any)[Symbol.iterator]=method;
  const actions:[string,()=>unknown][]=[
   ['for-of',()=>{for(const value of array)console.log('unexpected',value);}],
   ['spread',()=>[...array]],
   ['destructure',()=>{const [first]=array;return first;}],
   ['empty destructure',()=>{const []=array;}],
   ['set',()=>new Set(array)],
   ['iterator',()=>Iterator.from(array).next()]
  ];
  for(const [name,action] of actions){try{action();console.log(name,'unexpected success');}catch(error){console.log(name,(error as Error).name);}}
  try{for await(const value of array)console.log('unexpected async',value);}catch(error){console.log('for-await',(error as Error).name);}
 }
 let reads=0;
 const prototype={get [Symbol.iterator](){reads++;return null;}};
 Object.setPrototypeOf(prototype,Array.prototype);
 const inherited=[1,2];Object.setPrototypeOf(inherited,prototype);
 try{new Set(inherited);}catch(error){console.log('inherited',(error as Error).name,reads);}
 console.log('array-like fallback',Array.from(inherited).join(','));
 const iterator=[3,4].values();(iterator as any)[Symbol.iterator]=null;
 console.log('iterator fallback',Iterator.from(iterator)===iterator,Iterator.from(iterator).next().value);
}
run();
