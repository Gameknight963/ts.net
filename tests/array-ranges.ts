function ring(values: number[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        values[i % 4] += 1;
        total += values[i & 3];
        if (i === 7) break;
    }
    return total;
}
const values = [1, 2, 3, 4];
console.log(ring(values, 7), values[0], values[3]);
console.log(ring(values, 2.5), ring(values, 0), ring(values, -1));
console.log(ring(values, 0 / 0), ring(values, 2147483648), ring(values, 1 / 0));
const short = [1];
console.log(ring(short, 3), short.length, short[1]);
const holes: number[] = [1];
holes.length = 4;
console.log(ring(holes, 4), holes[1], holes[1] === undefined);
const generic: any[] = [1, 2, 3, 4];
console.log(ring(generic, 4), generic[0], generic[3]);
function read(items: any[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        if (typeof items[i % 4] === "number") total++;
        if (items[i & 3] === undefined) total += 10;
    }
    return total;
}
console.log(read([1, "two", , undefined], 11), read(generic, 11), read(values, 11));
function mixedRanges(items: number[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) total += items[i] + items[i & 3];
    return total;
}
console.log(mixedRanges([1, 2], 3), mixedRanges([1, 2, 3, 4], 4));
function maskForms(items: number[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) total += items[3 & i] + items[(i % 7) & 3] + items[i % 1];
    return total;
}
console.log(maskForms([1, 2, 3, 4], 30));
function outside(items: number[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) total += items[i & 7];
    return total;
}
console.log(outside([1, 2, 3, 4], 7));
function empty(count: number) {
    const items = undefined as unknown as number[];
    let total = 0;
    for (let i = 0; i < count; i++) total += items[0];
    return total;
}
console.log(empty(0), empty(-1), empty(0 / 0));
function missingWrite(items: number[], value: number, count: number) {
    for (let i = 0; i < count; i++) items[i % 4] = value;
}
missingWrite(values, undefined as unknown as number, 4);
console.log(values[0], values[3], ring(values, 4));
function nanWrite(items: number[], count: number) {
    for (let i = 0; i < count; i++) items[i & 3] = 0 / 0;
}
nanWrite(values, 4);
console.log(values[0], values[0] === undefined);
