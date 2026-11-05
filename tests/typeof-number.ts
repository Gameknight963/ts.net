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
