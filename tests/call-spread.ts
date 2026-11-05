function add(a: number, b = 7, c = 9) { return a + b + c; }
console.log(add(...[1, 2, 3] as const), add(...[1] as const));
const tuple: [number, number] = [4, 5];
console.log(add(...tuple));
function sum(...values: number[]) { let total = 0; for (const value of values) total += value; return total; }
const values = [1.5, 2.5, 3.5];
console.log(sum(...values), sum(1, ...values, 5, ...[6, 7]), sum(...[]));
function prefix(first: string, ...rest: number[]) { console.log(first, rest.length, rest[0], rest[1]); }
prefix(...["hello", 3, 4] as const);
prefix("empty", ...[]);
function destructured(...[a = 1, b = 2]: number[]) { return a + b; }
console.log(destructured(...[3, 4]), destructured(...[]));
function strings(...text: string[]) { console.log(text.length, text[0], text[1], text[2]); }
strings(..."A🦀B");
console.log(...[3, "hello", undefined, null]);
console.log(...[1, , 3]);
const output: number[] = [0];
console.log(output.push(...values, 5), output.length, output[1], output[4]);
console.log(output.push(...output), output.length, output[5]);
console.log(output.pop(...[] as []), output.length);
function advance(state: {x: number}) { state.x++; return state.x; }
function change(input: number[], state: {x: number}) { input[0] = 9; return advance(state); }
const state = {x: 0};
const input = [1, 2];
console.log(sum(...input, change(input, state), ...input), state.x);
function defaults(state: {x: number}, first = advance(state), ...rest: number[]) { console.log(first, rest[0], state.x); }
defaults(state, ...[undefined, advance(state)] as [undefined, number]);
function reference(...points: {x: number}[]) { points[0].x++; points.pop(); return points.length; }
const point = {x: 3};
const points = [point, point];
console.log(reference(...points), point.x, points.length);
function mixed(...args: any[]) { console.log(args.length, args[0], args[1]); }
function nothing(): void {}
mixed(...[nothing(), "done"]);
function flat(...[a, b]: number[]) { console.log(a, b); }
flat(...[1, 2]);
flat(...[1, ...[2, 3]]);
flat(...[]);
flat(...[undefined, 7] as any[]);
flat(...[advance(state), advance(state)], advance(state));
console.log(state.x);
add(...[advance(state), advance(state), advance(state)] as const);
console.log(state.x);
