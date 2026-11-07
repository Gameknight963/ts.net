const array=[10,20,30];console.log([...array.keys()].join("|"),[...array.values()].join("|"),JSON.stringify([...array.entries()]));
const iterator=array.values();console.log(iterator.next().value);array.push(40);array[1]=21;console.log(JSON.stringify(Array.from(iterator)));console.log(JSON.stringify(iterator.next()));array.push(50);console.log(JSON.stringify(iterator.next()));
const sparse:any[]=[,1,,];console.log(JSON.stringify([...sparse.values()]),JSON.stringify([...sparse.entries()]),[...sparse.keys()].join("|"));
for(const method of [Array.prototype.keys,Array.prototype.values,Array.prototype.entries] as any[]){const object:any={0:"a",2:"c",length:3};const iter=method.call(object);console.log(JSON.stringify(iter.next()));object.length=2;console.log(JSON.stringify(Array.from(iter)));}
const borrowed:any=Array.prototype.values;const units=Array.from(borrowed.call("\ud83d\ude00")) as string[];console.log("units",units.length,units[0].charCodeAt(0),units[1].charCodeAt(0));
const inherited:any[]=[,1];Object.setPrototypeOf(inherited,Object.assign(Object.create(Array.prototype),{0:"inherited"}));console.log([...inherited.values()].join("|"));
const shared=array.keys();console.log(shared.next().value);for(const key of shared){console.log("reuse",key);break;}console.log(shared.next().value);
for(const method of [Array.prototype.keys,Array.prototype.values,Array.prototype.entries])console.log("metadata",method.name,method.length);
