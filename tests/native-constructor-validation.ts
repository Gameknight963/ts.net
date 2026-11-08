export {};
function Alternate() {}
const bound = Alternate.bind(null);
let reads = 0;
Object.defineProperty(bound, 'prototype', {get() {reads++; return Alternate.prototype;}});
for (const constructor of [Uint8Array, Float64Array, BigInt64Array]) {
    for (const input of [-1, Symbol('length'), 1n]) {
        reads = 0;
        try {Reflect.construct(constructor, [input], bound); console.log('unexpected');}
        catch (error) {console.log('invalid length', constructor.name, (error as Error).name, reads);}
    }
    reads = 0;
    const value = Reflect.construct(constructor, [2], bound);
    const prototype = Object.getPrototypeOf(constructor.prototype);
    const length = Object.getOwnPropertyDescriptor(prototype, 'length')!.get!.call(value);
    console.log('length', constructor.name, reads, length, Object.getPrototypeOf(value) === Alternate.prototype);
}
for (const target of [null, undefined, 1, 'text', Symbol.for('registered')]) {
    reads = 0;
    try {Reflect.construct(WeakRef, [target], bound); console.log('unexpected');}
    catch (error) {console.log('weak target', (error as Error).name, reads);}
}
reads = 0;
const target = {};
const reference = Reflect.construct(WeakRef, [target], bound);
console.log('weak slots', reads, WeakRef.prototype.deref.call(reference) === target, Object.getPrototypeOf(reference) === Alternate.prototype);
let order = '';
const ordered = Alternate.bind(null);
Object.defineProperty(ordered, 'prototype', {get() {order += 'prototype|'; return Alternate.prototype;}});
const source = {get [Symbol.iterator]() {order += 'iterator|'; return undefined;}, get length() {order += 'length|'; return 1;}, get 0() {order += 'value|'; return 3;}};
const arrayLike = Reflect.construct(Uint8Array, [source], ordered);
console.log('object order', order, Object.getPrototypeOf(arrayLike) === Alternate.prototype);
order = '';
const view = Reflect.construct(Uint8Array, [new ArrayBuffer(4), {valueOf() {order += 'offset|'; return 0;}}], ordered);
console.log('buffer order', order, Object.getPrototypeOf(view) === Alternate.prototype);
order = '';
const iterable = {get [Symbol.iterator]() {order += 'iterator|'; return function*() {order += 'value|'; yield 3; order += 'done|';};}};
const collected = Reflect.construct(Uint8Array, [iterable], ordered);
console.log('iterable order', order, Uint8Array.prototype.join.call(collected, ','));
order = '';
const copied = Reflect.construct(Uint8Array, [new Uint8Array([7])], ordered);
console.log('copy order', order, Uint8Array.prototype.join.call(copied, ','));
