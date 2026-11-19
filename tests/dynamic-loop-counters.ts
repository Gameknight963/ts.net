export {};
function sum(callbacks: Array<() => number>): number {
    let result = 0;
    for (let i = 0; i < callbacks.length; i++) result += callbacks[i]();
    return result;
}
const shrinking = [() => { shrinking.length = 1; return 7; }, () => 20];
console.log('shrink', sum(shrinking));
const growing = [() => { if (growing.length === 2) growing.push(() => 30); return 10; }, () => 20];
console.log('grow', sum(growing));
let source = [() => { source = [() => 40, () => 50, () => 60]; return 3; }, () => 4];
function rebound(): number { let result = 0; for (let i = 0; i < source.length; i++) result += source[i](); return result; }
console.log('rebind', rebound());

let reads = 0;
const fractional = { get length() { reads++; return 2.5; } };
const positions: number[] = [];
for (let i = 0; i < fractional.length; i++) positions.push(i);
console.log('fractional', positions.join(','), reads);
function countLength(bound: any): number {
    const view = { length: bound };
    let count = 0;
    for (let i = 0; i < view.length; i++) count++;
    return count;
}
for (const bound of [-1, 0, 0.5, NaN, undefined, 2.5]) console.log('bound', bound, countLength(bound));

const sparse: number[] = [];
sparse.length = 2147483649;
sparse[2147483646] = 4; sparse[2147483647] = 5; sparse[2147483648] = 6;
const large: number[] = [];
let largeSum = 0;
for (let i = 2147483646; i < sparse.length; i++) { large.push(i); largeSum += sparse[i]; }
console.log('overflow', large.join(','), largeSum);
let edgeReads = 0;
const edge = { get length() { edgeReads++; return 2147483648.5; } };
const edgeValues: number[] = [];
for (let i = 2147483646; i < edge.length; i++) edgeValues.push(i);
console.log('overflow condition', edgeValues.join(','), edgeReads);

const callbacks = [() => 1, () => 2, () => 3, () => 4];
let total = 0;
outer: for (let i = 0; i < callbacks.length; i++) {
    for (let j = 0; j < 3; j++) {
        if (i === 1) continue outer;
        if (i === 3) break outer;
        total += callbacks[i]();
    }
}
console.log('labels', total);
let partial = 0;
try {
    const throwing = [() => 8, () => { throw new Error('stop'); }];
    for (let i = 0; i < throwing.length; i++) partial += throwing[i]();
} catch { console.log('exception', partial); }
const captured: Array<() => number> = [];
for (let i = 0; i < callbacks.length; i++) captured.push(() => i);
console.log('captures', captured.map(read => read()).join(','));
var visible = 0;
for (var visible = 0; visible < callbacks.length; visible++) { }
console.log('visible', visible);
for (let i = -0; i < callbacks.length; i++) { console.log('negative zero', Object.is(i, -0)); break; }
