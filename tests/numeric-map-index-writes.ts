export {};
function mapped(): number[] {return [1, 2, 3, 4].map(value => value + 1);}
function write(values: number[], index: number, value: number): void {values[index] = value;}
const values = mapped();
for (const index of [0, 3, -1, 0.5, NaN, Infinity]) write(values, index, 17);
console.log('values', JSON.stringify(values));
for (const index of [-1, 0.5, NaN, Infinity]) console.log('property', index, values[index]);
write(values, 1, undefined as any);
console.log('undefined', Object.hasOwn(values, 1), JSON.stringify(values));
const reversed = mapped(); reversed.reverse(); write(reversed, 0, -0);
console.log('materialized', Object.is(reversed[0], -0), JSON.stringify(reversed));
const events: string[] = [];
const inherited = mapped(); delete inherited[1];
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '1', {set(value: number) {events.push('set:' + value);}, configurable: true});
Object.setPrototypeOf(inherited, prototype); write(inherited, 1, 29);
console.log('setter', events.join(','), Object.hasOwn(inherited, 1));
const defined = mapped();
Object.defineProperty(defined, '1', {set(value: number) {events.push('own:' + value);}, configurable: true});
write(defined, 1, 31); console.log('own', events.join(','));
for (const transform of [(array: number[]) => {Object.freeze(array); return array;}, (array: number[]) => {Object.defineProperty(array, '0', {writable: false}); return array;}]) {
    const fixed = transform(mapped());
    try {write(fixed, 0, 41);} catch {console.log('rejected', fixed[0]);}
}
const sparse = mapped(); write(sparse, 2147483647, 42); write(sparse, 2147483648, 43);
console.log('sparse', sparse.length, sparse[2147483647], sparse[2147483648], Object.hasOwn(sparse, 2147483648));
