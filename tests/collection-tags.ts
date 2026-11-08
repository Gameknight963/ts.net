/// <reference lib="esnext.iterator" />
export {};
const map = new Map(), set = new Set();
for (const value of [map, set, map.values(), set.values()]) {
    const prototype = Object.getPrototypeOf(value);
    const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', Object.prototype.toString.call(value), Object.prototype.toString.call(prototype));
    console.log('descriptor', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
    console.log('fake', Object.prototype.toString.call(Object.create(prototype)));
    try {prototype[Symbol.toStringTag] = 'changed';} catch (error) {console.log('readonly', (error as Error).name);}
    delete prototype[Symbol.toStringTag];
    console.log('deleted', Object.prototype.toString.call(value), Object.prototype.toString.call(prototype));
    prototype[Symbol.toStringTag] = 'custom';
    console.log('recreated', Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!.enumerable, Object.prototype.toString.call(value));
}
const iteratorTag = Object.getOwnPropertyDescriptor(Iterator.prototype, Symbol.toStringTag)!;
console.log('iterator descriptor', typeof iteratorTag.get, typeof iteratorTag.set, iteratorTag.enumerable, iteratorTag.configurable);
for (const receiver of [null, undefined, 1, Iterator.prototype]) {
    console.log('getter', iteratorTag.get!.call(receiver));
    try {iteratorTag.set!.call(receiver, 'changed');} catch (error) {console.log('setter error', (error as Error).name);}
}
const child: any = Object.create(Iterator.prototype);
child[Symbol.toStringTag] = 'child';
console.log('child', Object.prototype.toString.call(child), Object.getOwnPropertyDescriptor(child, Symbol.toStringTag)!.enumerable);
