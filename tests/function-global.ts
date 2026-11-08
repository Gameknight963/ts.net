export {};
function ordinary(value: number) {return value + 1;}
const arrow = (value: number) => value * 2;
class Example {}
for (const value of [ordinary, arrow, Example, Array, Math.abs, ordinary.bind(null), Function, Function.prototype, {}, null, 1]) {
    console.log('instance', typeof value, value instanceof Function);
}
console.log('identity', ordinary.constructor === Function, arrow.constructor === Function, Example.constructor === Function, Function.constructor === Function, Function.prototype.constructor === Function, globalThis.Function === Function);
console.log('prototype', Object.getPrototypeOf(Function) === Function.prototype, Object.getPrototypeOf(ordinary) === Function.prototype, Object.getPrototypeOf(Function.prototype) === Object.prototype);
console.log('empty prototype', Function.prototype(), Function.prototype.call(null));
console.log('borrowed', Function.prototype.call.call(ordinary, null, 4), Function.prototype.apply.call(arrow, null, [3]), Function.prototype.bind.call(ordinary, null, 6)());
console.log('metadata', Function.name, Function.length, Function.prototype.name, Function.prototype.length);
for (const [target, key] of [[Function, 'prototype'], [Function.prototype, 'constructor']] as [any, string][]) {
    const descriptor = Object.getOwnPropertyDescriptor(target, key)!;
    console.log('descriptor', key, descriptor.value === (key === 'prototype' ? Function.prototype : Function), descriptor.writable, descriptor.enumerable, descriptor.configurable);
}
console.log('constructor keys', Object.keys(Function.prototype).includes('constructor'));
const source = {};
Object.setPrototypeOf(source, Function.prototype);
console.log('prototype based', source instanceof Function, typeof source);

const saved = Function.prototype.constructor;
Function.prototype.constructor = function Replacement() {};
console.log('replacement', ordinary.constructor === Function.prototype.constructor, arrow.constructor === Function.prototype.constructor);
Function.prototype.constructor = saved;
console.log('restored', ordinary.constructor === Function);
try {(Function as any).prototype = {};} catch (error) {console.log('readonly prototype', (error as Error).name);}
console.log('prototype retained', Object.getPrototypeOf(ordinary) === Function.prototype);
