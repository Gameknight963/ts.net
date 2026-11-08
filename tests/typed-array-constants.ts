export {};
for (const constructor of [Int8Array, Uint8Array, Uint8ClampedArray, Int16Array, Uint16Array, Int32Array, Uint32Array, Float32Array, Float64Array, BigInt64Array, BigUint64Array]) {
    for (const target of [constructor, constructor.prototype]) {
        const descriptor = Object.getOwnPropertyDescriptor(target, 'BYTES_PER_ELEMENT')!;
        console.log('constant', constructor.name, descriptor.value, descriptor.writable, descriptor.enumerable, descriptor.configurable);
        console.log('reject', Reflect.set(target, 'BYTES_PER_ELEMENT', 99), Reflect.deleteProperty(target, 'BYTES_PER_ELEMENT'), Reflect.defineProperty(target, 'BYTES_PER_ELEMENT', {value: 99}));
        console.log('same', Reflect.defineProperty(target, 'BYTES_PER_ELEMENT', {value: descriptor.value}), Reflect.defineProperty(target, 'BYTES_PER_ELEMENT', {}));
    }
}
class Derived extends Uint8Array {}
console.log('derived', Derived.BYTES_PER_ELEMENT, new Derived(1).BYTES_PER_ELEMENT, Object.hasOwn(Derived, 'BYTES_PER_ELEMENT'));
