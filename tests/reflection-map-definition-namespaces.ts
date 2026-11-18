export {};
const methods=new Map([['unused',Object.defineProperty]]);
const namespaces=new Map([['object',Object]]);
function lookup<K,V>(map:ReadonlyMap<K,V>,key:K):V|undefined {return map.get(key);}
const target={value:1};
lookup(namespaces,'object')!.defineProperty(target,'value',{value:9,enumerable:false});
console.log(target.value,Object.keys(target).length);
