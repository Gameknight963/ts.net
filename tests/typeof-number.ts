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

function looseClassify(value: any): number {
    let result = 0;
    if (typeof value == "number") result += 1;
    if ("number" == typeof value) result += 2;
    if (typeof value != "number") result += 4;
    if ("number" != typeof value) result += 8;
    console.log('loose values', typeof value == "number", "number" == typeof value,
        typeof value != "number", "number" != typeof value);
    return result;
}
for (const value of [2, NaN, Infinity, -0, undefined, null, false, "2", {}, [], 1n, Symbol()])
    console.log('loose branch', looseClassify(value));
function looseOptional(value?: number): boolean {return typeof value == "number";}
console.log('loose optional', looseOptional(), looseOptional(3), looseOptional(NaN));
console.log('loose effects', typeof next() == "number", "number" != typeof next(), visits);
console.log('loose void', typeof nothing() == "number", visits);
const looseItems: any[] = [3, 'text', , undefined, NaN];
let looseGets = 0;
Object.defineProperty(looseItems, '1', {get() {looseGets++; return 4;}, configurable: true});
for (let i=0;i<looseItems.length;i++) {
    console.log('loose array', typeof looseItems[i] == "number", "number" != typeof looseItems[i]);
}
console.log('loose getters', looseGets);

async function looseAwait(): Promise<void> {
    console.log('loose await start');
    console.log('loose await number', typeof await Promise.resolve(3) == "number");
    console.log('loose await text', "number" != typeof await Promise.resolve('text'));
    console.log('loose await missing', typeof await Promise.resolve(undefined) == "number");
}
looseAwait();
console.log('loose await scheduled');
function* looseYield(): Generator<number, boolean, any> {
    const first = typeof (yield 1) == "number";
    const second = "number" != typeof (yield 2);
    return first && second;
}
const looseIterator = looseYield();
console.log('loose yield', JSON.stringify(looseIterator.next()), JSON.stringify(looseIterator.next(3)), JSON.stringify(looseIterator.next('text')));
