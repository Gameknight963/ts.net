export {};
function sum(values: unknown[], initial: number = 0): number {
    let total = initial;
    for (const value of values) {
        if (typeof value === 'number') total += value;
    }
    return total;
}
console.log(sum([1, 'skip', 2, undefined, null, false, 3]));
console.log(sum([1, , 3]), sum([]), Object.is(sum([], -0), -0));
console.log(Object.is(sum(['skip'], -0), -0), Object.is(sum([0], -0), -0));
console.log(sum([NaN]), sum([Infinity, -Infinity]), sum([1e20, -1e20, 1]));
const inherited: unknown[] = [1, , 3];
Object.defineProperty(Array.prototype, '1', {value: 7, configurable: true, writable: true});
try { console.log(sum(inherited)); }
finally { delete (Array.prototype as any)[1]; }
const getter: unknown[] = [1, 2, 3];
Object.defineProperty(getter, '1', {get() { console.log('get'); return 8; }});
console.log(sum(getter));
const custom: unknown[] = [1, 2];
custom[Symbol.iterator] = function* (): Generator<unknown, undefined, unknown> { console.log('iterate'); yield 9; yield 'skip'; yield 4; return undefined; };
console.log(sum(custom));
let last: unknown;
let total = 0;
for (last of [1, 'last']) { if (typeof last === 'number') total += last; }
console.log(total, last);
function withCall(values: unknown[]): number {
    let result = 0;
    for (const value of values) { if (typeof value === 'number') { console.log(value); result += value; } }
    return result;
}
console.log(withCall([2, 'skip', 4]));
