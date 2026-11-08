export {};
const promise = Promise.resolve(42), prototype = Promise.prototype;
const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
console.log('descriptor', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
for (const value of [promise, prototype, Object.create(prototype)]) console.log('brand', Object.prototype.toString.call(value));
try {(prototype as any)[Symbol.toStringTag] = 'changed';} catch (error) {console.log('readonly', (error as Error).name);}
console.log('keys', Object.keys(prototype).length);
for (const key of ['then', 'catch', 'finally', 'constructor']) {
    const descriptor = Object.getOwnPropertyDescriptor(prototype, key)!;
    console.log('method', key, descriptor.writable, descriptor.enumerable, descriptor.configurable);
}
delete (prototype as any)[Symbol.toStringTag];
console.log('deleted', Object.prototype.toString.call(promise), Object.prototype.toString.call(prototype));
(prototype as any)[Symbol.toStringTag] = 'custom';
console.log('recreated', Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!.enumerable, Object.prototype.toString.call(promise));
promise.then(value => console.log('resolved', value));
