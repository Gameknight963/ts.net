export {};
function sum(values: number[]): number {
    let total = 0;
    for (const value of values) {
        const twice = value * 2;
        if (value > 1) total += twice;
        else total -= value;
    }
    return total;
}
console.log('packed', sum([1, 2, 3]), sum([1, 2, 3].map(value => value)));
console.log('empty', sum([]), sum(Object.freeze([1, 2, 3]) as unknown as number[]));
console.log('special', sum([0, -0, Infinity]), Number.isNaN(sum([NaN])));
console.log('undefined', Number.isNaN(sum([1, undefined as unknown as number, 3])));
const sparse = [1, 2, 3]; delete sparse[1];
console.log('hole', Number.isNaN(sum(sparse)));
let reads = 0;
const getter = [1, 2, 3];
Object.defineProperty(getter, '1', { get() { reads++; return 5; }, configurable: true });
console.log('getter', sum(getter), reads);
const inherited = [1, 2, 3]; delete inherited[1];
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '1', { get() { reads++; return 7; }, configurable: true });
Object.setPrototypeOf(inherited, prototype);
console.log('prototype', sum(inherited), reads);
const custom = [1, 2, 3];
custom[Symbol.iterator] = function*() { yield 10; yield 20; return undefined; };
console.log('iterator', sum(custom));
let lookups = 0;
const lookup = [1, 2, 3];
Object.defineProperty(lookup, Symbol.iterator, { get() { lookups++; lookup[0] = 9; return Array.prototype.values; } });
console.log('lookup', sum(lookup), lookups);
function last(values: number[]): number { let value = 7; for (value of values) {} return value; }
console.log('binding', last([]), last([2, 3]));
function variable(values: number[]): number { var value = 9; for (var value of values) { const twice = value * 2; } return value; }
console.log('var', variable([]), variable([4, 5]));
function mutate(values: number[]): number {
    let total = 0;
    for (const value of values) { if (value === 1) values.push(4); total += value; }
    return total;
}
console.log('mutation', mutate([1, 2, 3]));
function captured(values: number[]): number {
    let total = 0;
    const read = () => total;
    for (const value of values) total += value;
    return read();
}
console.log('captured', captured([2, 3, 4]));
