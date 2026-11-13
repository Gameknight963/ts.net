export {};
function sum(values: number[]): number {
    let total = 0; for (let i = 0; i < values.length; i++) total += values[i]; return total;
}
function mapped(): number[] {return [1, 2, 3, 4].map(value => value + 1);}
console.log('packed', sum([1, 2, 3, 4]), sum(mapped()), sum([]));
const frozen = [1, 2, 3, 4]; Object.freeze(frozen); console.log('frozen', sum(frozen));
const reversed = mapped(); reversed.reverse(); console.log('materialized', sum(reversed));
const special = [NaN, -0, Infinity]; console.log('special', sum(special));
const missing = mapped(); delete missing[1]; console.log('missing', sum(missing));
const undefinedValue = mapped(); undefinedValue[1] = undefined as any; console.log('undefined', sum(undefinedValue));
let reads = 0;
const defined = [1, 2, 3, 4];
Object.defineProperty(defined, '1', {get() {reads++; defined.length = 2; return 7;}, configurable: true});
console.log('getter', sum(defined), reads, defined.length);
const inherited = mapped(); delete inherited[1];
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '1', {get() {reads++; inherited.length = 2; return 9;}, configurable: true});
Object.setPrototypeOf(inherited, prototype);
console.log('inherited', sum(inherited), reads, inherited.length);
function withCallback(values: number[], callback: () => void): number {
    let total = 0; for (let i = 0; i < values.length; i++) {total += values[i]; callback();} return total;
}
const changed = mapped(); console.log('callback', withCallback(changed, () => {changed.length = 1;}));
const written = mapped(), alias: any = written;
console.log('cached', alias[0]);
let before = 0; for (let i = 0; i < written.length; i++) {before += written[i]; written[i] += 10;}
console.log('writes', before, alias[0], JSON.stringify(written));
function typedSum(values: Float64Array): number {
    let total = 0; for (let i = 0; i < values.length; i++) total += values[i]; return total;
}
const typed = new Float64Array([1, 2, 3, 4]); console.log('typed', typedSum(typed));
let lengths = 0; Object.defineProperty(typed, 'length', {get() {lengths++; return 2;}});
console.log('custom length', typedSum(typed), lengths);

for (const length of [15, 16, 17, 31, 32, 33, 63, 64, 65]) {
    const input: number[] = []; for (let i = 0; i < length; i++) input.push(1);
    const values = input.map(value => value + 1);
    console.log('range', length, sum(values));
    values[length - 1] = undefined as any;
    console.log('last undefined', length, sum(values));
    values.length = length - 1;
    console.log('shortened', length, sum(values));
}
