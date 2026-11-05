function objectParameter({x, y = x + 1}: {x: number; y?: number}, next = y + 1) { return x + y + next; }
console.log(objectParameter({x: 3}), objectParameter({x: 2, y: 8}, 4));
function arrayParameter([first, , third = first + 2, ...rest]: number[], next = third + 1) {
    return first + third + next + rest.length;
}
console.log(arrayParameter([1, 9]), arrayParameter([2, 9, 5, 6, 7], 10));
function defaults({x} = {x: 3}, y = x + 1, [z] = [y + 1]) { return x + y + z; }
console.log(defaults(), defaults(undefined, 8), defaults({x: 2}, undefined, [9]));
function nested({point: {x}, values: [y = x]}: {point: {x: number}; values: number[]}) { return x + y; }
console.log(nested({point: {x: 4}, values: []}));
function characters([first, second, third = "end"]: string) { console.log(first, second.length, third); }
characters("A🦀");
function mutate({x}: {x: number}, y = x++) { return x + y; }
console.log(mutate({x: 3}));
function redeclare([x] = [1]) { var x = 9; return x; }
console.log(redeclare());
function increment(state: {x: number}) { state.x++; return state.x; }
function ordered(state: {x: number}, [x = increment(state)]: number[], last = 0) { return x + last; }
// Arguments run before any parameter initializer.
function argumentOrder({x = 7}: {x?: number}, state: {x: number}) { return x + state.x; }
const state = {x: 0};
console.log(argumentOrder({x: increment(state)}, state), state.x);
console.log(ordered(state, [], increment(state)), state.x);
function alias({point}: {point: {x: number}}) { point.x++; }
const point = {x: 4};
alias({point});
console.log(point.x);
function noBindings({}, []: number[]) { return 8; }
console.log(noBindings(1, []));
function variants({x}: {x: number}) {
    var total = 1;
    var total = 3;
    for (let i = 0; i < 5; i++) { const temporary = {value: i}; total += temporary.value + x; }
    return total;
}
const input = {x: 2};
console.log(variants({x: 2}), variants(input), variants({x: 3}));
function optional({x}: {x?: number}) { return x === undefined ? 7 : x; }
console.log(optional({}), optional({x: 3}), optional({x: undefined}));
function recursive({n}: {n: number}): number { return n === 0 ? 0 : n + recursive({n: n - 1}); }
console.log(recursive({n: 5}));
function literalOrder({first, second, state}: {first: number; second: number; state: {x: number}; extra?: number}) {
    return first * 100 + second * 10 + state.x;
}
console.log(literalOrder({second: increment(state), first: increment(state), extra: increment(state), state}), state.x);
