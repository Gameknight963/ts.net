type Pair = {a: number; b: number};
function make(first: boolean): Pair {
    if (first) return {b: 2, a: 1};
    return {b: 4, a: 3};
}
const first = make(true), second = make(false);
console.log(Object.keys(first)[0], Object.keys(second)[1], first.a, second.a, first === second);
const extra: any = first;
extra.z = 5;
console.log(Object.keys(first).length, Object.keys(second).length);
