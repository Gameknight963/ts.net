let total = 0;
for (const value of [1, 2, 3]) total += value;
console.log(total);
for (const value of [1, , 3]) console.log(value, typeof value);
for (const value of [true, false]) console.log(value);
for (const point of [{ x: 4 }, { x: 5 }]) console.log(point.x);
for (const row of [[1, 2], [3]]) console.log(row[0]);
const dynamic: any = [1, "two", undefined];
for (const value of dynamic) console.log(value);
for (const character of "a\u{1f980}\u03bb") console.log(character, character.length);
for (const character of "\u{1f980}"[0]) console.log(character.length);
for (const character of "e\u0301") console.log(character.length);
const dynamicString: any = "a\u{1f980}b";
for (const character of dynamicString) console.log(character.length);

const growing = [1, 2, 3];
for (const value of growing) {
    if (value === 1) growing.push(4);
    if (value === 2) growing[2] = 30;
    console.log(value);
}
const shrinking = [1, 2, 3];
for (const value of shrinking) { console.log(value); shrinking.length = 1; }
let original = [1, 2, 3];
for (const value of original) { console.log(value); original = []; }
let existing = 7;
for (existing of []) {}
console.log(existing);
for (existing of [8, 9]) {}
console.log(existing);
var reused = 7;
for (var reused of [] as number[]) {}
console.log(reused);
for (var reused of [8, 9]) {}
console.log(reused);
for (const existing of [1, 2]) console.log(existing);
console.log(existing);
function reusedNested() {
    var result = 7;
    for (const row of [[3], []]) {
        for (var result of row) {}
        console.log(result);
    }
    return result;
}
console.log(reusedNested());
function parameter(value: number) { for (value of [2, 3]) {} return value; }
console.log(parameter(7));

type Point = { x: number };
const target = { x: 0 };
for (target.x of [4, 5]) console.log(target.x);
console.log(target.x);
function source(counter: Point) { counter.x++; return [7, 8]; }
function index(values: number[], counter: Point) { counter.x++; values.length = 0; return 0; }
const calls = { x: 0 };
const input = source(calls);
const output: number[] = [];
for (output[index(input, calls)] of input) {}
console.log(calls.x, output[0], output.length);
const lengths = [2, 1, 0];
for (lengths.length of lengths) {}
console.log(lengths.length);
let visits = 0;
outer: for (const first of [1, 2, 3]) {
    for (const second of [4, 5, 6]) {
        if (second === 5) continue outer;
        if (first === 3) break outer;
        visits += first + second;
    }
}
console.log(visits);
function returns(values: number[]) { for (const value of values) return value; }
console.log(returns([3]), returns([]));
function counterWrite() {
    let visits = 0;
    for (let i = 0; i < 3; i++) {
        for (i of [undefined] as any[]) {}
        visits++;
    }
    return visits;
}
console.log(counterWrite());
function boundWrite(count: number) {
    let visits = 0;
    for (let i = 0; i < count; i++) { for (count of [0]) {} visits++; }
    return visits;
}
console.log(boundWrite(4));
function packedBarrier(values: number[], count: number) {
    for (let i = 0; i < count; i++) {
        for (values.length of [1]) {}
        values[i] += 1;
    }
}
const barrier = [1, 2, 3];
packedBarrier(barrier, 3);
console.log(barrier[0], barrier[1], barrier[2], barrier.length);
