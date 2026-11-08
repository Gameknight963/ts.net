/// <reference lib="es2024" />
export {};
const array = new Uint8Array([1, 2]);
console.log('element', Reflect.defineProperty(array, '0', {value: 258, writable: true, enumerable: true, configurable: true}), array[0]);
for (const descriptor of [{writable: false}, {enumerable: false}, {configurable: false}, {get() {return 9;}}]) {
    console.log('invalid flags', Reflect.defineProperty(array, '0', descriptor));
}
for (const key of ['-0', '-1', '2', 'NaN', 'Infinity', '0.5']) console.log('invalid index', key, Reflect.defineProperty(array, key, {value: 9}));
Object.defineProperty(array, '01', {value: 3, enumerable: true});
Object.defineProperty(array, 'hidden', {value: 4});
console.log('keys', Object.keys(array).join(','), Object.getOwnPropertyNames(array).join(','), (array as any)['01']);
Object.preventExtensions(array);
console.log('closed', Object.isExtensible(array), Object.isSealed(array), Object.isFrozen(array), Reflect.defineProperty(array, '1', {value: 5}), Reflect.defineProperty(array, 'newKey', {value: 6}));
console.log('values', array.join(','), Reflect.deleteProperty(array, '0'));
const nonempty = new Uint8Array([1]);
try {Object.freeze(nonempty);} catch (error) {console.log('freeze', (error as Error).name, Object.isExtensible(nonempty), Object.isSealed(nonempty), Object.isFrozen(nonempty));}
const empty = Object.freeze(new Uint8Array(0));
console.log('empty', Object.isFrozen(empty), Reflect.defineProperty(empty, 'extra', {value: 1}));
const resizable = new ArrayBuffer(4, {maxByteLength: 8});
const tracking = new Uint8Array(resizable);
console.log('resizable', Reflect.preventExtensions(tracking), Object.isExtensible(tracking));
try {Object.preventExtensions(tracking);} catch (error) {console.log('resizable error', (error as Error).name);}
const shared = new SharedArrayBuffer(4, {maxByteLength: 8});
console.log('shared tracking', Reflect.preventExtensions(new Uint8Array(shared)));
const fixed = new Uint8Array(shared, 0, 2);
console.log('shared fixed', Reflect.preventExtensions(fixed), Object.isSealed(fixed));
const sealing = new Uint8Array([1]);
try {Object.seal(sealing);} catch (error) {console.log('seal', (error as Error).name, Object.isSealed(sealing));}
console.log('seal again', Object.seal(sealing) === sealing);
const bigint = new BigInt64Array(1);
Object.defineProperty(bigint, '0', {value: 11n});
console.log('bigint', String(bigint[0]));
try {Object.defineProperty(bigint, '0', {value: 1});} catch (error) {console.log('bigint conversion', (error as Error).name);}
const detachedBuffer = new ArrayBuffer(4);
const detached = new Uint8Array(detachedBuffer);
const converting = {valueOf() {detachedBuffer.transfer(); return 4;}};
console.log('detach conversion', Reflect.defineProperty(detached, '0', {value: converting}), detached.length);
const large = new Uint8Array(1000000);
Object.preventExtensions(large);
console.log('large integrity', Object.isSealed(large), Object.isFrozen(large));
