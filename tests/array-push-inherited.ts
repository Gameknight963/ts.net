export {};
function run(array: any[]): void {
    const prototype = Object.create(Array.prototype);
    let seen = '';
    Object.defineProperty(prototype, '0', {set(value) {seen += value;}, configurable: true});
    Object.setPrototypeOf(array, prototype);
    console.log('push', array.push(3, 4), array.length, seen, Object.hasOwn(array, '0'), array[1]);
}
run([]);
const numeric: number[] = [];
let total = 0;
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '0', {set(value: number) {total += value; numeric.length = 5;}});
Object.setPrototypeOf(numeric, prototype);
console.log('numeric', numeric.push(7), numeric.length, total, Object.hasOwn(numeric, '0'));
const blocked: number[] = [];
const readOnly = Object.create(Array.prototype);
Object.defineProperty(readOnly, '0', {value: 1, writable: false});
Object.setPrototypeOf(blocked, readOnly);
try {blocked.push(2); console.log('unexpected');} catch (error) {console.log('blocked', (error as Error).name, blocked.length);}
