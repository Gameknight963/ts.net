export {};
let define: typeof Object.defineProperty;
define = Object.defineProperty;
const first = {a: 1};define(first,'a',{value: 10});console.log('assignment', first.a);

let operations: typeof Object;
operations = Object;
const second = {b: 2};operations.seal(second);
console.log('namespace', Object.isSealed(second), Object.getOwnPropertyDescriptor(second,'b')?.configurable);

let optional: typeof Object.freeze | undefined;
optional ??= Object.freeze;
const third = {c: 3};optional(third);console.log('nullish', Object.isFrozen(third));

let prevent: typeof Object.preventExtensions;
const fourth = {d: 4};(prevent = Object.preventExtensions)(fourth);console.log('expression', Object.isExtensible(fourth));

let reflected: typeof Reflect;
reflected = Reflect;
const fifth = {e: 5};console.log('reflect', reflected.defineProperty(fifth,'e',{value: 20}), fifth.e);

function replace(target: {f: number}, operation: typeof Object.defineProperty): void {
    operation = Object.defineProperty;operation(target,'f',{value: 30});
}
const sixth = {f: 6};replace(sixth,(target => target) as typeof Object.defineProperty);console.log('parameter assignment', sixth.f);
