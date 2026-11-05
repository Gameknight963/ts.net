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

const stable = Number.isInteger;
const alias = stable;
console.log(alias(2), alias(2.5), alias.name, stable === alias);
const unknownResult = Number.isFinite as (value: unknown) => unknown;
const boxedResult = unknownResult(3);
console.log(boxedResult, typeof boxedResult);
console.log(indirect(3), typeof indirect(3));
let changing = Number.isFinite;
console.log(changing(2));
changing = Number.isNaN;
console.log(changing(2), changing(NaN));
function defaultPredicate(check = Number.isFinite, value: unknown = 3): boolean { return check(value); }
console.log(defaultPredicate(), defaultPredicate(Number.isNaN, 2), defaultPredicate(Number.isNaN, NaN));
const discarded: (value: unknown) => void = Number.isFinite;
discarded(2);
console.log(alias(...[2] as [unknown]));

function observeHoisted(): void { console.log(typeof hoistedCheck); }
observeHoisted();
var hoistedCheck = Number.isFinite;
console.log(hoistedCheck(2));

function storedLoop(count: number, seed: number): number {
    const originalCheck = Number.isInteger;
    const forwarded = originalCheck;
    let total = 0;
    for (let i = 0; i < count; i++) if (forwarded(i)) total += 1;
    return total + seed;
}
console.log(storedLoop(8, 7));
