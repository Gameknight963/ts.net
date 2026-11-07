let hints = "";
const value: any = {[Symbol.toPrimitive](hint: string) { hints += hint + ","; return hint === "string" ? "name" : 7; }, valueOf() { throw "wrong fallback"; }};
console.log("number", Number(value), +value, value * 2);
console.log("string", String(value), `${value}`);
console.log("default", value + 1, value == 7, 7 == value);
console.log("compare", value < 8, value >= 7);
const record: any = {}; record[value] = 12;
console.log("key", record.name, hints);
const symbol = Symbol("key");
const symbolic: any = {[Symbol.toPrimitive]() { return symbol; }};
record[symbolic] = 13;
console.log("symbol", record[symbol], symbolic == symbol, symbol == symbolic);
let calls = 0;
const getter: any = {get [Symbol.toPrimitive]() { calls++; console.log("getter receiver", this === getter); return function(this: any, hint: string) {console.log("call receiver", this === getter, hint); return 3; }; }};
console.log("getter", Number(getter), calls);
for (const method of [1, () => ({}), () => { throw "conversion failure"; }]) {
    try { Number({[Symbol.toPrimitive]: method}); } catch (error) { console.log("error", typeof error === "string" ? error : (error as Error).name); }
}
for (const method of [undefined, null]) console.log("fallback", Number({[Symbol.toPrimitive]: method, valueOf() {return 4; }}));
class CustomDate extends Date { [Symbol.toPrimitive](hint: string): any { return hint === "number" ? 5 : "date"; } }
const date: any = new CustomDate(0);
console.log("date", Number(date), String(date), date + 1);
async function formatCustom(): Promise<void> {
    const awaited: any = {[Symbol.toPrimitive](hint: string) { return hint + " value"; }};
    console.log("await template", `${await Promise.resolve(awaited)}`);
}
formatCustom();
