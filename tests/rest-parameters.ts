function sum(...values: number[]) { let total = 0; for (const value of values) total += value; return total; }
console.log(sum(), sum(1), sum(1, 2, 3.5));
function defaults(first = 4, ...values: number[]) { return first + values.length + (values[0] ?? 0); }
console.log(defaults(), defaults(undefined), defaults(undefined, 3, 5), defaults(2, 7));
function destructured(...[first = 7, second = first + 1, ...tail]: number[]) { return first + second + tail.length; }
console.log(destructured(), destructured(1), destructured(2, 4, 8, 9));
function objectPattern(...{length}: number[]) { return length; }
console.log(objectPattern(), objectPattern(1, 2, 3));
function anyObjectPattern(...{length}: any[]) { return length; }
console.log(anyObjectPattern({x: 1}), anyObjectPattern({x: 1}, {x: 2}));
function mixed(...values: any[]) { console.log(values.length, values[0], values[1], values[2], values[3]); }
mixed(3, "hello", undefined, null);
mixed();
function strings(...values: string[]) { return `${values.length}:${values[0] ?? "empty"}:${values[1] ?? "end"}`; }
console.log(strings(), strings("A", "🦀"));
function flags(...values: boolean[]) { return values[0] === true && values[1] === false; }
console.log(flags(true, false));
function alias(...values: {x: number}[]) { values[0].x++; values.pop(); return values.length; }
const point = {x: 2};
console.log(alias(point, point), point.x);
function fresh(...values: number[]) { values.push(9); return values.length; }
console.log(fresh(), fresh(), fresh(1));
function advance(state: {x: number}) { state.x++; return state.x; }
function ordered(state: {x: number}, first = advance(state), ...values: number[]) {
    console.log(first, values[0], values[1], state.x);
}
const state = {x: 0};
ordered(state, undefined, advance(state), advance(state));
function tuple(...values: [number, string]) { return `${values[0]}:${values[1]}`; }
console.log(tuple(3, "hello"));
function nothing(): void {}
mixed(nothing());
function reassign(...values: number[]) { values = [8, 9]; return values[0]; }
console.log(reassign(1));
function flat(...[a, b]: number[]) { return `${a}:${b}:${typeof a}`; }
console.log(flat(), flat(1), flat(1, 2, 3));
console.log(flat(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17));
console.log(flat(advance(state), advance(state), advance(state)), state.x);
function flatTuple(...[a, b]: [number, string]) { return `${a}:${b}`; }
console.log(flatTuple(3, "hello"));
function flatAny(...[a, b]: any[]) { console.log(a, b); }
flatAny(nothing(), "done");
function none(...[]: number[]) { return 3; }
console.log(none(), none(1, 2));
