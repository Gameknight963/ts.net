export {};
const target = {}, reference = new WeakRef(target), prototype = WeakRef.prototype;
const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
console.log('descriptor', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
for (const value of [reference, prototype, Object.create(prototype)]) console.log('brand', Object.prototype.toString.call(value));
try {(prototype as any)[Symbol.toStringTag] = 'changed';} catch (error) {console.log('readonly', (error as Error).name);}
console.log('deref', reference.deref() === target, Object.keys(prototype).length);
delete (prototype as any)[Symbol.toStringTag];
console.log('deleted', Object.prototype.toString.call(reference), Object.prototype.toString.call(prototype));
(prototype as any)[Symbol.toStringTag] = 'custom';
console.log('recreated', Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!.enumerable, Object.prototype.toString.call(reference));
