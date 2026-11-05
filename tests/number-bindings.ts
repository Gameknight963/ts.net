import { integerCheck, finiteCheck } from "./modules/number-bindings.js";
export {};
const { isNaN, isFinite: finite, isInteger: integer, isSafeInteger: safe } = Number;
console.log(isNaN(NaN), finite(Infinity), integer(2), integer(2.5), safe(9007199254740992));
console.log(isNaN === Number.isNaN, finite === Number.isFinite, integer.name, safe.length);
let effects = 0;
function fallback(): (value: unknown) => boolean { effects++;return Number.isNaN; }
const { isInteger: withDefault = fallback() } = Number;
console.log(withDefault(2), effects);
const { ["isFinite"]: computed, [`isInteger`]: template } = Number;
console.log(computed(3), template(2.5));
const { isInteger: { name: functionName, length: arity } } = Number;
console.log(functionName, arity);
let { isInteger: mutable } = Number;
console.log(mutable(2));
mutable = Number.isNaN;
console.log(mutable(2), mutable(NaN));
function shadowed(Number: { isInteger: (value: unknown) => boolean }): boolean {
    const { isInteger: check } = Number;
    return check(2);
}
console.log(shadowed({ isInteger: value => false }));
function observeHoisted(): void { console.log(typeof hoisted); }
observeHoisted();
var { isFinite: hoisted } = Number;
console.log(hoisted(3));
function destructuredLoop(count: number, seed: number): number {
    const { isInteger: check } = Number;
    const forwarded = check;
    let total = 0;
    for (let i = 0; i < count; i++) if (forwarded(i)) total += 1;
    return total + seed;
}
console.log(destructuredLoop(8, 7));

console.log(integerCheck(2), finiteCheck(Infinity), integerCheck === Number.isInteger);
