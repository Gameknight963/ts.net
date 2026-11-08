export {};
for (const value of [new WeakMap(), new WeakSet()]) {
    const prototype = Object.getPrototypeOf(value);
    const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('descriptor', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
    console.log('brands', Object.prototype.toString.call(value), Object.prototype.toString.call(prototype), Object.prototype.toString.call(Object.create(prototype)));
    try {prototype[Symbol.toStringTag] = 'changed';} catch (error) {console.log('readonly', (error as Error).name);}
    delete prototype[Symbol.toStringTag];
    console.log('deleted', Object.prototype.toString.call(value));
    prototype[Symbol.toStringTag] = 'custom';
    console.log('recreated', Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!.enumerable, Object.prototype.toString.call(value));
}
