export {};
const methods=new Map([['unused',Reflect.deleteProperty]]);
const namespaces=new Map([['reflect',Reflect]]);
function lookup<K,V>(map:ReadonlyMap<K,V>,key:K):V|undefined {return map.get(key);}
const target={value:1,keep:2};
console.log(lookup(namespaces,'reflect')!.deleteProperty(target,'value'),Object.keys(target).join(','));
const indexed={value:2,keep:3};
const key:string='deleteProperty';
console.log((lookup(namespaces,'reflect')! as any)[key](indexed,'value'),Object.keys(indexed).join(','));
