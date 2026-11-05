function test(value: any) {
    console.log(typeof value === "number", "number" === typeof value, typeof value !== "number", "number" !== typeof value);
}
test(2); test(0 / 0); test(1 / 0); test(-0); test(undefined); test(null); test(false); test("2"); test({}); test([]);
function optional(value?: number) { console.log(typeof value === "number", typeof value !== "number"); }
optional(); optional(3); optional(0 / 0);
let visits = 0;
function next(): any { visits++; return visits === 1 ? 3 : "x"; }
console.log(typeof next() === "number", typeof next() !== "number", visits);
function nothing(): void { visits++; }
console.log(typeof nothing() === "number", visits);

function classify(value: any): number {
    let result = 0;
    if (typeof value === "number") result += 1;
    if ("number" === typeof value) result += 2;
    if (typeof value !== "number") result += 4;
    if ("number" !== typeof value) result += 8;
    return result;
}
for (const value of [2, NaN, Infinity, -0, undefined, null, false, "2", {}, []]) console.log(classify(value));
if (typeof nothing() === "number") console.log("wrong");
else console.log("void", visits);
if ("number" !== typeof next()) console.log("text", visits);
function optionalCondition(value?: number): number {
    if (typeof value === "number") return 1;
    return 2;
}
console.log(optionalCondition(), optionalCondition(3), optionalCondition(NaN));
