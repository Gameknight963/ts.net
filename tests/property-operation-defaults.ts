export {};

function define(target: object, operation = Object.defineProperty): void {
    operation(target, 'a', {value: 4, enumerable: true, configurable: true, writable: true});
}
const defined = {a: 5}; define(defined);
console.log('define default', defined.a, Object.getOwnPropertyDescriptor(defined, 'a')?.writable);

function reflectDefine(target: object, operation = Reflect.defineProperty): boolean {
    return operation(target, 'a', {value: 6, configurable: true});
}
const reflected = {a: 7};
console.log('reflect default', reflectDefine(reflected), reflected.a);

function freeze(target: object, operation = Object.freeze): void { operation(target); }
const frozen = {a: 8}; freeze(frozen);
console.log('freeze default', Object.isFrozen(frozen), Reflect.set(frozen, 'a', 9), frozen.a);

function seal(target: object, namespace = Object): void { namespace.seal(target); }
const sealed = {a: 10}; seal(sealed);
console.log('namespace default', Object.isSealed(sealed), Object.getOwnPropertyDescriptor(sealed, 'a')?.configurable);

function prevent(target: object, operation = Object.preventExtensions): void {operation(target);}
const prevented = {a: 12}; prevent(prevented);
console.log('prevent default', Object.isExtensible(prevented), Reflect.set(prevented, 'b', 13));

const alias = Object.defineProperties;
function defineMany(target: object, operation = alias): void {
    operation(target, {a: {value: 16, enumerable: true}});
}
const many = {a: 17}; defineMany(many);
console.log('alias default', many.a);
