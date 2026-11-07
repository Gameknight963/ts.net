const display=Object.getPrototypeOf(Math.abs).toString;
for(const method of [Math.abs,JSON.parse,JSON.stringify,Object.prototype.toString,Object.getOwnPropertyDescriptor(Map.prototype,'size')!.get,Object.getOwnPropertyDescriptor(Set.prototype,'size')!.get,Object.getOwnPropertyDescriptor(Object.prototype,'__proto__')!.get,Object.getOwnPropertyDescriptor(Object.prototype,'__proto__')!.set,Math.abs.bind(null),Math.abs.bind(null).bind(null)]) {
    console.log('native',display.call(method));
}
delete (Object.getPrototypeOf(Math.abs) as any).name;
const absolute:any=Math.abs;
const original=absolute.name;
delete absolute.name;absolute.name='renamed';
console.log('renamed',absolute.name,display.call(absolute));
delete absolute.name;absolute.name=original;
const bound:any=Math.abs.bind(null);
delete bound.name;bound.name='ordinary';console.log('bound renamed',display.call(bound));
const proto=Object.getOwnPropertyDescriptor(Object.prototype,'__proto__')!;
console.log('accessor metadata',proto.get!.name,proto.get!.length,proto.set!.name,proto.set!.length);
for(const value of [null,undefined,{},3])try{display.call(value);}catch(error){console.log('invalid',(error as Error).name);}

