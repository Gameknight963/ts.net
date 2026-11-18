export {};
function increment(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total++;
    return total;
}
function decrement(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) --total;
    return total;
}
function add(count: number, seed: number): number {
    let total = seed;
    for (let i = 2; i < count; i++) total += 2;
    return total;
}
function subtract(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total -= 3;
    return total;
}
function assigned(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total = 2 + total;
    return total;
}
class Counter {value = 0; constructor(seed: number) {this.value = seed;}}
function field(count: number, seed: number): number {
    const target = new Counter(seed), alias = target;
    for (let i = 0; i < count; i++) alias.value++;
    return target.value;
}
function fieldSubtract(count: number, seed: number): number {
    const target = new Counter(seed);
    for (let i = 0; i < count; i++) target.value = target.value - 2;
    return target.value;
}
function step(value: number): number {return value + 3;}
function fieldCall(count: number, seed: number): number {
    const target = new Counter(seed);
    for (let i = 0; i < count; i++) target.value = step(target.value);
    return target.value;
}
for (const count of [-1, 0, 0.1, 1, 31, 32, 33.1, 64, NaN]) {
    for (const seed of [-0, -2, 0.5, NaN, Infinity, -Infinity, -2147483648, -2147483584, -2147483520, 2147483455, 2147483583, 2147483646, 2147483647, 9007199254740992, undefined]) {
        for (const kernel of [increment, decrement, add, subtract, assigned, field, fieldSubtract, fieldCall]) {
            const result = kernel(count, seed as number);
            console.log(result, result === 0 ? 1 / result : 0);
        }
    }
}
let reads = 0, writes = 0;
class Accessors {
    stored = 0;
    get value(): number {reads++; return this.stored;}
    set value(next: number) {writes++; this.stored = next;}
}
const accessor = new Accessors();
for (let i = 0; i < 64; i++) accessor.value++;
console.log('accessors', accessor.stored, reads, writes);
const frozen = Object.freeze(new Counter(1)) as Counter;
try {for (let i = 0; i < 64; i++) frozen.value++;} catch {console.log('frozen', frozen.value);}
function early(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) {total++; if (i === 2) break;}
    return total;
}
console.log('early exit', early(64), early(2147483648));
function zeroStep(count: number): number {
    let value = -0;
    for (let i = 0; i < count; i++) value -= 0;
    return 1 / value;
}
console.log('zero step', zeroStep(0), zeroStep(64));
function positiveZero(count: number): number {
    let value = -0;
    for (let i = 0; i < count; i++) value += 0;
    return 1 / value;
}
console.log('positive zero', positiveZero(0), positiveZero(64));
function otherField(count: number): number {
    const first = new Counter(1), second = new Counter(2);
    for (let i = 0; i < count; i++) first.value = second.value + 1;
    return first.value;
}
function otherFieldCall(count: number): number {
    const first = new Counter(1), second = new Counter(2);
    for (let i = 0; i < count; i++) first.value = step(second.value);
    return first.value;
}
console.log('other location', otherField(64), otherFieldCall(64));
