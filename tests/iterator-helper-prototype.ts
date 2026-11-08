/// <reference lib="esnext.iterator" />
export {};
const helpers = [Iterator.from([1]).map(x => x), Iterator.from([1]).filter(() => true), Iterator.from([1]).take(1), Iterator.from([1]).drop(1), Iterator.from([1]).flatMap(x => [x])];
const prototype = Object.getPrototypeOf(helpers[0]);
for (const helper of helpers) console.log('prototype', Object.getPrototypeOf(helper) === prototype, Object.prototype.toString.call(helper));
console.log('parent', Object.getPrototypeOf(prototype) === Iterator.prototype);
for (const iterator of [helpers[0], 'a'.matchAll(/a/g)]) {
    const parent = Object.getPrototypeOf(iterator);
    const descriptor = Object.getOwnPropertyDescriptor(parent, Symbol.toStringTag)!;
    console.log('tag descriptor', descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
    try {parent[Symbol.toStringTag] = 'changed';} catch (error) {console.log('readonly', (error as Error).name);}
    for (const key of ['next', 'return']) {
        const method = Object.getOwnPropertyDescriptor(parent, key);
        if (method) console.log('method', key, method.writable, method.enumerable, method.configurable, method.value.name, method.value.length);
    }
}
for (const key of ['next', 'return']) for (const receiver of [null, undefined, {}, [1].values()]) {
    try {prototype[key].call(receiver);} catch (error) {console.log('brand', key, (error as Error).name);}
}
