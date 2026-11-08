export {};
const array: number[] = new Array(3);
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '0', {value: 9});
Object.defineProperty(prototype, '2', {get() {throw new Error('must not read');}});
Object.setPrototypeOf(array, prototype);
for (let index = 0; index < 4; index++) console.log('numeric', index, index in array, String(index) in array, Object.hasOwn(array, index));
const mixed: (number | string)[] = [1, 'x'];
delete mixed[0];
Object.setPrototypeOf(mixed, prototype);
console.log('mixed', 0 in mixed, 1 in mixed, 2 in mixed);
const standard: number[] = [];
Object.defineProperty(Array.prototype, '100', {get() {throw new Error('must not read');}, configurable: true});
let present = false;
try {present = 100 in standard;} finally {Reflect.deleteProperty(Array.prototype, '100');}
console.log('standard', present, 100 in standard);
