export {};
function fresh(): number[] { return [0, -0, NaN, Infinity, 2, 3, 4].map(value => value); }
const filtered = fresh().filter(value => Number.isNaN(value) || value >= 0);
console.log(filtered.length, filtered.join(','), Object.is(filtered[1], -0), Array.isArray(filtered));
console.log(Object.getPrototypeOf(filtered) === Array.prototype, Object.keys(filtered).join(','));
filtered.push(9); filtered[0] = 8;
console.log(filtered.join(','), JSON.stringify(filtered));
console.log(Object.freeze(fresh()).filter(value => value > 2).join(','));
for (const mode of ['delete', 'shrink', 'getter', 'undefined', 'prototype']) {
    const source = [1, 2, 3, 4].map(value => value);
    let reads = 0, calls = 0;
    const result = source.filter((value, index) => {
        calls++;
        if (index === 0) {
            if (mode === 'delete') delete source[1];
            if (mode === 'shrink') source.length = 1;
            if (mode === 'getter') Object.defineProperty(source, '1', { get() { reads++; return 7; }, configurable: true });
            if (mode === 'undefined') source[1] = undefined as unknown as number;
            if (mode === 'prototype') {
                delete source[1];
                const prototype = Object.create(Array.prototype);
                Object.defineProperty(prototype, '1', { get() { reads++; return 11; }, configurable: true });
                Object.setPrototypeOf(source, prototype);
            }
        }
        return value === undefined || value >= 0;
    });
    console.log(mode, result.length, result.join(','), calls, reads, Object.hasOwn(result, '1'));
}
const custom: any = fresh();
const destination: any[] = [];
function Result(this: any, length: number): any { console.log('species', length); return destination; }
custom.constructor = { [Symbol.species]: Result };
const result = custom.filter((value: number) => value > 2);
console.log(result === destination, destination.join(','));
const empty = fresh().filter(value => value < 0);
console.log(empty.length, Object.keys(empty).length);
let setterCalls = 0;
const ownSource = [2, 4, 6].map(value => value);
let ownResult: number[] = [];
Object.defineProperty(Array.prototype, '0', { set() { setterCalls++; }, configurable: true });
try { ownResult = ownSource.filter(value => value > 0); }
finally { delete (Array.prototype as any)[0]; }
console.log('own result', ownResult.join(','), setterCalls, Object.hasOwn(ownResult, '0'));
