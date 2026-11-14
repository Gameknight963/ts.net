export {};
function choose(test: boolean, first: any, second: any): number { return test ? first : second; }
for (const value of [3, -0, NaN, Infinity, undefined]) {
    const a = choose(true, value, "unused"), b = choose(false, "unused", value);
    console.log("selected", String(a), String(b), Object.is(a, -0), Object.is(b, -0));
}
const log: string[] = [];
function condition(): boolean { log.push("condition"); return true; }
function selected(): any { log.push("selected"); return 7; }
function skipped(): any { throw new Error("skipped"); }
const stored: number = condition() ? selected() : skipped();
console.log("order", stored, log.join(","));
function nested(first: boolean, second: boolean, value: unknown): number { return first ? second ? value as any : 11 : 19; }
console.log("nested", nested(true, true, 3), nested(true, false, "unused"), nested(false, true, "unused"));
function arithmetic(value: any): number { return typeof value === "number" ? value + 7 : 0; }
console.log("arithmetic", arithmetic(3), arithmetic("3"), arithmetic(undefined));
let calls = 0;
const state = { get value(): any { calls++; return 7; } };
const assigned: number[] = [true ? state.value : skipped(), false ? skipped() : state.value];
console.log("getters", assigned.join(","), calls);
