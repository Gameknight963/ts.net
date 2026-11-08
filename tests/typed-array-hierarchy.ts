export {};
const base: any = Object.getPrototypeOf(Uint8Array);
const prototype = Object.getPrototypeOf(Uint8Array.prototype);
console.log('base', base.name, base.length, base.prototype === prototype, prototype.constructor === base);
for (const constructor of [Uint8Array, Int16Array, Float64Array, BigInt64Array]) {
    console.log('shared', Object.getPrototypeOf(constructor) === base, Object.hasOwn(constructor, 'from'), Object.hasOwn(constructor, 'of'), Object.hasOwn(constructor, Symbol.species), constructor.from === base.from, constructor.prototype.map === prototype.map);
}
console.log('factory', base.of.call(Uint8Array, 1, 2).join(','), base.from.call(Int16Array, [3, 4]).join(','));
console.log('species', base[Symbol.species] === base, (Uint8Array as any)[Symbol.species] === Uint8Array);
try {new base(1); console.log('unexpected');} catch (error) {console.log('abstract', (error as Error).name);}
try {base(); console.log('unexpected');} catch (error) {console.log('call', (error as Error).name);}
console.log('iterator', prototype[Symbol.iterator] === prototype.values, Uint8Array.prototype.values === Float64Array.prototype.values);
const original = prototype.join;
Object.defineProperty(prototype, 'join', {value: function() {return 'shared';}, writable: true, configurable: true});
console.log('override', new Uint8Array(1).join(','), new Float64Array(1).join(','));
Object.defineProperty(prototype, 'join', {value: original});
console.log('restore', new Uint8Array([3, 4]).join(','));
