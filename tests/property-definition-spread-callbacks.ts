export {};
function define(target: {a: number}, operation: typeof Object.defineProperty): void {operation(target,'a',{value: 10});}
const first = {a: 1};define(...[first,Object.defineProperty] as const);console.log('literal', first.a);
function namespace(target: {b: number}, operations: typeof Object): void {operations.freeze(target);}
const second = {b: 2};namespace(...[second,Object] as const);console.log('namespace', Object.isFrozen(second));
function nested(target: {c: number}, operation: typeof Object.seal): void {operation(target);}
const third = {c: 3};nested(...[...[third] as const,...[Object.seal] as const] as const);console.log('nested', Object.isSealed(third));
class Operation {
    constructor(target: {d: number}, operation: typeof Object.preventExtensions) {operation(target);}
}
const fourth = {d: 4};new Operation(...[fourth,Object.preventExtensions] as const);console.log('constructor', Object.isExtensible(fourth));
