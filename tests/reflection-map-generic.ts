export {};
const operations=new Map<string,typeof Reflect.deleteProperty>();operations.set('erase',Reflect.deleteProperty);
function lookup<K,V>(map:ReadonlyMap<K,V>,key:K):V|undefined {return map.get(key);}
const target={generic:1,keep:2};
console.log(lookup(operations,'erase')!(target,'generic'),Object.hasOwn(target,'generic'),Object.keys(target).join(','));
