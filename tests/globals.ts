function readHoisted() { return hoisted; }
console.log(readHoisted());
var hoisted = 3;
console.log(readHoisted());
var hoisted: number;
console.log(readHoisted());
let count = 1;
function increment() { return ++count; }
function readCount() { return count; }
console.log(increment(), readCount());
function setCount(value: number) { count = value; }
setCount(8);
console.log(readCount());
const point = {x: 1};
const alias = point;
function changePoint() { alias.x++; return point.x; }
console.log(changePoint(), alias.x);
const values = [1, 2];
function append() { values.push(3); return values.length; }
console.log(append(), values[2]);
let flag: boolean | undefined;
function toggle() { flag = !flag; return flag; }
console.log(flag, toggle(), flag);
let reference: string | undefined;
function referenceValue() { return reference; }
console.log(referenceValue());reference = "hello";console.log(referenceValue());
const [first, second = first + 1] = [4];
function bindings() { return first + second; }
console.log(bindings());
var loopValue = 7;
function loopRead() { return loopValue; }
for (var loopValue of [] as number[]) {}
console.log(loopRead());
for (var loopValue of [2, 3]) {}
console.log(loopRead());
var [destructured] = [5];
function readBinding() { return destructured; }
console.log(readBinding());
const limit = 5;
function bounded() { let total = 0; for (let i = 0; i < limit; i++) total += i; return total; }
console.log(bounded());
let mutableBound = 5;
function lowerBound() { mutableBound = 0; }
function changingBound() { let visits = 0; for (let i = 0; i < mutableBound; i++) { lowerBound(); visits++; } return visits; }
console.log(changingBound());
function shadow(count: number) { count++; return count; }
console.log(shadow(2), readCount());
let dynamic: any;
function dynamicValue() { return dynamic; }
console.log(dynamicValue());dynamic = null;console.log(dynamicValue());
var accumulator: number | undefined;
function cachedLoop(bound: number) {
    for (let i = 0; i < bound; i++) {
        accumulator = (accumulator ?? 0) + (i & 3);
        if (i === 1) continue;
        if (i === 4) break;
    }
    return accumulator;
}
console.log(cachedLoop(0), cachedLoop(0 / 0), cachedLoop(-1));
console.log(cachedLoop(2.5), accumulator, cachedLoop(9), accumulator);
function earlyReturn() {
    for (let i = 0; i < 3; i++) { accumulator = i; if (i === 1) return accumulator; }
    return accumulator;
}
console.log(earlyReturn(), accumulator);
function observe() { return accumulator; }
function withCall() {
    for (let i = 0; i < 3; i++) { accumulator = i; if (observe() !== i) return -1; }
    return 3;
}
console.log(withCall(), accumulator);
var remaining = 3;
function repeated() { let visits = 0; while (remaining > 0) { remaining--; visits++; } return visits; }
console.log(repeated(), remaining, repeated(), remaining);
