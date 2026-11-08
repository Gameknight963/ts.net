export {};
const prototype = Object.getPrototypeOf(() => 0);
const original = prototype.call;
Object.defineProperty(prototype, 'call', {enumerable: true});
console.log('enumerable', Object.keys(prototype).join(','), prototype.call === original);
Object.defineProperty(prototype, 'call', {enumerable: false, writable: false});
try {prototype.call = () => 0;} catch (error) {console.log('readonly', (error as Error).name);}
console.log('preserved', prototype.call === original, Object.keys(prototype).length);
const descriptor = Object.getOwnPropertyDescriptor(prototype, Symbol.hasInstance)!;
console.log('instance descriptor', descriptor.writable, descriptor.enumerable, descriptor.configurable);
console.log('same instance method', Reflect.defineProperty(prototype, Symbol.hasInstance, {value: descriptor.value}));
console.log('replace instance method', Reflect.defineProperty(prototype, Symbol.hasInstance, {value: () => true}));
Object.freeze(prototype);
console.log('frozen', Object.isFrozen(prototype), Reflect.deleteProperty(prototype, 'bind'), Reflect.defineProperty(prototype, 'newMethod', {value: () => 0}));
function example(a: number, b: number) {return a + b;}
console.log('callable', example.call(null, 2, 3), example.bind(null, 4)(5));
