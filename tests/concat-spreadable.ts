const concat: any = Array.prototype.concat;
const array: any = [1, 2]; array[Symbol.isConcatSpreadable] = false;
const kept = concat.call([], array);
console.log("kept", kept.length, kept[0] === array);
const indexed = {0: "a", 2: "c", length: 3, [Symbol.isConcatSpreadable]: true};
console.log("spread", JSON.stringify(concat.call([0], indexed, 4)), Object.keys(concat.call([], indexed)).join(","));
console.log("receiver", JSON.stringify(concat.call(indexed, [4])));
const ancestor = {1: "inherited", [Symbol.isConcatSpreadable]: true};
const inherited = Object.assign(Object.create(ancestor), {0: "own", length: 2});
console.log("inherited", JSON.stringify(concat.call([], inherited)));
for (const flag of [undefined, null, false, true, 0, 1, "", "yes", {}]) {
    const value: any = [7]; value[Symbol.isConcatSpreadable] = flag;
    const result = concat.call([], value);
    console.log("flag", result[0] === value ? "kept" : "spread");
}
let effects = "";
const getter = {get [Symbol.isConcatSpreadable]() {effects += "S"; return true;}, get length() {effects += "L"; return 2;}, get 0() {effects += "0"; return "first";}, get 1() {effects += "1"; return "second";}};
console.log("getters", JSON.stringify(concat.call([], getter)), effects);
try { concat.call([], {get [Symbol.isConcatSpreadable]() {throw "spread failure";}}); } catch (error) {console.log("error", error);}
console.log("primitive", JSON.stringify(concat.call([], "abc", null, undefined)));
const boxed: any = Object("ab"); boxed[Symbol.isConcatSpreadable] = true;
console.log("boxed", JSON.stringify(concat.call([], boxed)));
