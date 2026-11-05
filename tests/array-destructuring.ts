const [first, , third, absent = 9] = [1, 2, 3];
console.log(first, third, absent);
const [empty = 4, present = 8] = [undefined, 0];
console.log(empty, present);
const [letter, crab, last, missing = "end"] = "A🦀B";
console.log(letter, crab, crab.length, last, missing);
const [head, ...tail] = [1, 2, , 4];
console.log(head, tail.length, tail[0], tail[1], tail[2]);
const [...characters] = "x🦀\ud800";
console.log(characters.length, characters[1].length, characters[2] === "\ud800");
const [[nested = 7], [other], ...remaining] = [[undefined], [3], [4, 5]];
console.log(nested, other, remaining.length, remaining[0][1]);
const [[fallback] = [6]] = [] as number[][];
console.log(fallback);
function alter(values: (number | undefined)[]) { values[1] = 5; return 3; }
const live = [undefined, 2];
const [changed = alter(live), following] = live;
console.log(changed, following);
function grow(values: number[]) { values.push(7); return 4; }
const short: number[] = [];
const [a = grow(short), b = 8, ...rest] = short;
console.log(a, b, rest.length, short.length);
const [point] = [{x: 1}];
const [alias] = [point];
alias.x = 9;
console.log(point.x);
function localLoop(count: number) {
    let sum = 0;
    for (let i = 0; i < count; i++) { const [a, b = a + 1] = [i]; sum += a + b; }
    return sum;
}
console.log(localLoop(5));
function redeclaration(count: number) {
    let visits = 0;
    for (let i = 0; i < count; i++) { var [count] = [0]; visits++; }
    return visits;
}
console.log(redeclaration(4));
const [nan, zero] = [0 / 0, -0];
console.log(nan, 1 / zero);
function advance(state: {x: number}) { state.x++; return state.x; }
const state = {x: 0};
const [early = advance(state), later] = [undefined, advance(state)];
console.log(early, later, state.x);
const [] = [advance(state), advance(state)];
console.log(state.x);
const [...[restFirst, restSecond]] = [4, 5];
console.log(restFirst, restSecond);
let pairTotal = 0;
for (const [left, right = 8] of [[1, 2], [3]]) pairTotal += left + right;
console.log(pairTotal);
var varLeft = 0;
var varRight = 0;
for (var [varLeft, varRight] of [[1, 2], [3, 4]]) {}
console.log(varLeft, varRight);
function hoistedBinding() {
    console.log(hoisted);
    var [hoisted]: any[] = [undefined];
    return hoisted;
}
console.log(hoistedBinding());
