/// <reference lib="esnext.iterator" />
export {};
for (const value of [[1].values(), 'a'[Symbol.iterator]()]) {
    const prototype = Object.getPrototypeOf(value);
    const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('descriptor', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
    try {prototype[Symbol.toStringTag] = 'changed';} catch (error) {console.log('readonly', (error as Error).name);}
    console.log('tag', Object.prototype.toString.call(value));
    console.log('delete', delete prototype[Symbol.toStringTag], Object.prototype.toString.call(value));
    prototype[Symbol.toStringTag] = 'custom';
    console.log('recreated', Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!.enumerable, Object.prototype.toString.call(value));
}
