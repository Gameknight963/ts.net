export {};
function* generate() {yield 1;}
async function* generateAsync() {yield 2;}
function owner(value: object): any {
    let current: any = Object.getPrototypeOf(value);
    while (!Object.hasOwn(current, Symbol.toStringTag)) current = Object.getPrototypeOf(current);
    return current;
}
for (const prototype of [Atomics, ArrayBuffer.prototype, DataView.prototype, SharedArrayBuffer.prototype, DOMException.prototype, owner(generate()), owner(generateAsync())]) {
    const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable, Reflect.set(prototype, Symbol.toStringTag, 'changed'));
    console.log('same', Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!.value === tag.value);
}
