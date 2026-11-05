function describe(value: any): string {
    let text = "";
    for (const key of Object.keys(value)) text += key + "|";
    return text;
}
console.log(describe({b: 2, a: 1}));
const contextual: {a: number; b: number} = {b: 2, a: 1};
console.log(describe(contextual));
const other: {a: number; b: number} = {a: 3, b: 4};
console.log(describe(other));
const narrow: {a: number} = contextual;
console.log(describe(narrow));
const dynamic: any = contextual;
dynamic.z = 3; dynamic.a = 9; dynamic.c = 4;
console.log(describe(dynamic), contextual.a);
console.log(describe({"10": 1, "2": 2, z: 3, "01": 4, "4294967295": 5, "0": 6}));
const optional: {a?: number; b: number} = {b: 2};
console.log(describe(optional));
optional.a = undefined;
console.log(describe(optional), Object.hasOwn(optional, "a"));
const numbers = [1, , undefined, 4];
console.log(describe(numbers));
const extras: any = numbers; extras.extra = 3;
console.log(describe(numbers));

const sparse: any[] = [];
sparse[4294967294] = 7;
sparse[3] = undefined;
console.log(describe(sparse));
console.log(describe("A??"), describe(3), describe(false));
const fn: any = (value: number) => value + 1;
fn.extra = 3;
console.log(describe(fn));
const keys = Object.keys;
console.log(keys.name, keys.length, keys === Object["keys"], typeof keys);
console.log(describe(keys(contextual)));
console.log(keys(contextual)[0], Object["keys"](contextual)[1]);
const keyList = Object.keys(contextual);
keyList[0] = "changed";
console.log(Object.keys(contextual)[0]);
function shadow(Object: {keys: (value: number) => string[]}) { return Object.keys(3)[0]; }
console.log(shadow({keys: value => ["shadow"]}));
let trace = "";
function mark(value: number): number { trace += value; return value; }
const loose: any = Object.keys;
console.log(loose({a: mark(1)}, mark(2))[0], trace);
console.log(loose(...[{x: 3}])[0]);
