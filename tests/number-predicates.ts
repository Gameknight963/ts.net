function show(value: unknown): void {
    console.log(Number.isNaN(value), Number.isFinite(value), Number.isInteger(value), Number.isSafeInteger(value));
}
for (const value of [undefined, null, false, true, "0", "NaN", {}, [], 0, -0, 1, -1, 1.5, 5e-324, NaN, Infinity, -Infinity, 1e300, 9007199254740991, 9007199254740992, -9007199254740991, -9007199254740992]) show(value);
function numeric(value?: number): number {
    let flags = 0;
    if (Number.isNaN(value)) flags += 1;
    if (Number.isFinite(value)) flags += 2;
    if (Number.isInteger(value)) flags += 4;
    if (Number.isSafeInteger(value)) flags += 8;
    return flags;
}
console.log(numeric(), numeric(NaN), numeric(Infinity), numeric(2), numeric(2.5));
console.log(Number.isNaN(NaN), Number.isFinite(Infinity), Number.isInteger(-0), Number.isSafeInteger(9007199254740991));
let effects = 0;
function record(value: unknown): unknown { effects++;return value; }
function nothing(): void { effects++; }
console.log(Number.isNaN(record(NaN)), Number.isFinite(nothing()), effects);
console.log(Number.isNaN(...[record(NaN)] as [unknown]), effects);
console.log(Number.isNaN(...([record(NaN), record("extra")] as unknown as [unknown])), effects);
console.log(Number.isNaN(...([] as unknown as [unknown])));
const predicates = [Number.isNaN, Number.isFinite, Number.isInteger, Number.isSafeInteger];
function apply(predicate: (value: unknown) => boolean, value: unknown): boolean { return predicate(value); }
for (const predicate of predicates) console.log(predicate.name, predicate.length, typeof predicate, apply(predicate, 2.5), apply(predicate, NaN));
console.log(Number.isNaN === Number.isNaN, Number.isNaN === Number["isNaN"], Number["isInteger"](2));
const indirect: any = Number.isFinite;
console.log(indirect(), indirect(2, record("extra")), indirect(...[2]), effects);
function shadowed(Number: { isNaN: (value: unknown) => boolean }): boolean { return Number.isNaN(2); }
console.log(shadowed({ isNaN: value => true }));

console.log(Number.isInteger((effects++, 2)), Number.isSafeInteger((record(3), effects++ | 0)), effects);
console.log(Number.isNaN(~NaN), Number.isFinite(~Infinity), Number.isSafeInteger((1 as any)));
let proved = 0;
for (let i = 0; i < 8; i++) {
    if (Number.isNaN(i)) proved += 1000;
    if (Number.isFinite(i)) proved += 1;
    if (Number.isInteger(i)) proved += 1;
    if (Number.isSafeInteger(i)) proved += 1;
}
console.log(proved);
