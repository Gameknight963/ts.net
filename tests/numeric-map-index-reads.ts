export {};
function mapped(): number[] { return [1, 2, 3, 4].map(value => value + 1); }
function read(values: number[], index: number): number { return values[index]; }
const values = mapped();
for (const index of [0, 1, 3, -1, 0.5, NaN, Infinity, 2147483648]) {
    console.log('read', index, read(values, index));
}
(values as any)[0.5] = 17;
(values as any)[-1] = 18;
console.log('properties', read(values, 0.5), read(values, -1));
Object.defineProperty(values, '1', {get() { return 23; }, configurable: true});
console.log('getter', read(values, 1));
delete values[2];
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '2', {get() { return 29; }, configurable: true});
Object.setPrototypeOf(values, prototype);
console.log('prototype', read(values, 2));
const reversed = mapped(); reversed.reverse();
console.log('materialized', read(reversed, 0), read(reversed, 3));
const special = mapped(); special[0] = -0; special[1] = NaN;
console.log('special', Object.is(read(special, 0), -0), Number.isNaN(read(special, 1)));
