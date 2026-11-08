export {};
function define(target: object, operation: typeof Object.defineProperty): void {operation(target, 'a', {value: 10});}
const first = {a: 1}; define(first, Object.defineProperty);
console.log('define', first.a);

function forward(target: object, operation: typeof Object.defineProperty): void {define(target, operation);}
const second = {a: 2}; forward(second, Object.defineProperty);
console.log('forward', second.a);

function namespace(target: object, operations: typeof Object): void {operations.freeze(target);}
const third = {a: 3}; namespace(third, Object);
console.log('namespace', Object.isFrozen(third), Object.getOwnPropertyDescriptor(third, 'a')?.writable);

function destructured(target: object, {seal}: typeof Object): void {seal(target);}
const fourth = {a: 4}; destructured(fourth, Object);
console.log('destructured', Object.isSealed(fourth), Object.getOwnPropertyDescriptor(fourth, 'a')?.configurable);

function generic<T extends object>(target: T, operation: typeof Reflect.defineProperty): boolean {
    return operation(target, 'a', {value: 20});
}
const fifth = {a: 5};
console.log('generic', generic(fifth, Reflect.defineProperty), fifth.a);

class Operation {
    constructor(target: object, operation: typeof Object.preventExtensions) {operation(target);}
}
const sixth = {a: 6}; new Operation(sixth, Object.preventExtensions);
console.log('constructor', Object.isExtensible(sixth));
