const {x, y: renamed, missing = 7}: {x: number; y: string; missing?: number} = {x: 3, y: "hello"};
console.log(x, renamed, missing);
const {point: {x: nested}, items: [first, second = 8]} = {point: {x: 4}, items: [2]};
console.log(nested, first, second);
const [{x: fromArray}] = [{x: 5}];
console.log(fromArray);
const {point: {y: fallback} = {y: 9}}: {point?: {y: number}} = {};
console.log(fallback);
const input: any = {x: undefined, y: 3};
function alter(value: any) { value.y = 5; return 4; }
const {x: initial = alter(input), y: following} = input;
console.log(initial, following);
const key = "value";
const {[key]: computed} = {value: 6};
console.log(computed);
const {length: arrayLength, 0: element} = [2, 3];
const {length: stringLength, 0: codeUnit} = "🦀x";
console.log(arrayLength, element, stringLength, codeUnit.length);
const {} = 3;
const {missing: primitiveDefault = 8} = false as any;
console.log(primitiveDefault);
const original = {x: 1};
const {alias} = {alias: original};
alias.x = 9;
console.log(original.x);
function kernel(count: number, seed: number) {
    let sum = 0;
    for (let i = 0; i < count; i++) { const {a, b} = {a: i + seed, b: i + 0.5}; sum += a + b; }
    return sum;
}
console.log(kernel(5, 1));
let total = 0;
for (const {x: value} of [{x: 2}, {x: 3}]) total += value;
console.log(total);
function hoisted() { console.log(value); var {value}: any = {}; return value; }
console.log(hoisted());
function advance(state: {x: number}) { state.x++; return state.x; }
const state = {x: 0};
const {a = advance(state), b}: {a?: number; b: number} = {b: advance(state)};
console.log(a, b, state.x);
const {} = {unused: advance(state)};
console.log(state.x);
type TreeA = {value: number; next?: TreeA};
type TreeB = {value: number; next?: TreeB};
const treeA: TreeA = {value: 1};
const treeB: TreeB = treeA;
console.log(treeB.value, treeB.next);
type NestedA = {point: {x: number}};
type NestedB = {point: {x: number}};
const nestedA: NestedA = {point: {x: 8}};
const nestedB: NestedB = nestedA;
console.log(nestedB.point.x);
