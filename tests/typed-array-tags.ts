export {};
const constructors = [Int8Array, Uint8Array, Uint8ClampedArray, Int16Array, Uint16Array, Int32Array, Uint32Array, Float32Array, Float64Array, BigInt64Array, BigUint64Array];
const shared = Object.getPrototypeOf(Uint8Array.prototype);
const descriptor = Object.getOwnPropertyDescriptor(shared, Symbol.toStringTag)!;
console.log('descriptor', typeof descriptor.get, descriptor.set === undefined, descriptor.enumerable, descriptor.configurable);
for (const constructor of constructors) {
    const value = new constructor(2);
    console.log('brand', constructor.name, Object.prototype.toString.call(value), Object.prototype.toString.call(constructor.prototype), Object.hasOwn(constructor.prototype, Symbol.toStringTag), Object.getPrototypeOf(constructor.prototype) === shared);
    console.log('getter', descriptor.get!.call(value), descriptor.get!.call(constructor.prototype) === undefined, Reflect.set(value, Symbol.toStringTag, 'changed'));
}
for (const value of [undefined, null, {}, [], new ArrayBuffer(0), new DataView(new ArrayBuffer(0))]) console.log('other', descriptor.get!.call(value) === undefined);
const shadow = new Uint8Array(1);
Object.defineProperty(shadow, Symbol.toStringTag, {value: 'custom', configurable: true});
console.log('shadow', Object.prototype.toString.call(shadow));
Reflect.deleteProperty(shadow, Symbol.toStringTag);
console.log('restore', Object.prototype.toString.call(shadow));
