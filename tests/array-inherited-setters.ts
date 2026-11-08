export {};
function check(array: any[], value: any): void {
    let seen: any = 'none';
    let receiver = false;
    const prototype = Object.create(Array.prototype);
    Object.defineProperty(prototype, '0', {set(input) {seen = input; receiver = this === array;}, configurable: true});
    Object.setPrototypeOf(array, prototype);
    array[0] = value;
    console.log('setter', seen, receiver, array.length, Object.hasOwn(array, '0'));
    Object.defineProperty(prototype, '1', {value: 7, writable: false});
    try {array[1] = value; console.log('unexpected');} catch (error) {console.log('readonly', (error as Error).name, array.length, Object.hasOwn(array, '1'));}
}
check([], 42);
check(new Array(3), 'text');
const numbers: number[] = [];
let total = 0;
const numericPrototype = Object.create(Array.prototype);
Object.defineProperty(numericPrototype, '0', {set(value: number) {total += value;}});
Object.setPrototypeOf(numbers, numericPrototype);
for (let i = 0; i < 3; i++) numbers[0] = i;
console.log('numbers', total, numbers.length, Object.hasOwn(numbers, '0'));
const mixed: (number | string)[] = [1, 'x'];
delete mixed[0];
check(mixed, 9);
const ordinary: number[] = [];
let defaultSeen = 0;
Object.defineProperty(Array.prototype, '100', {set(value: number) {defaultSeen = value;}, configurable: true});
try {ordinary[100] = 8;} finally {delete (Array.prototype as any)[100];}
console.log('default prototype', defaultSeen, ordinary.length, Object.hasOwn(ordinary, '100'));
const own = [1];
Object.setPrototypeOf(own, numericPrototype);
own[0] = 10;
console.log('own', own[0], total);
