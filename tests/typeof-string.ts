function test(value: any) {
    console.log(typeof value === "string", "string" === typeof value, typeof value !== "string", "string" !== typeof value);
}
test(2); test(0 / 0); test(1 / 0); test(-0); test(undefined); test(null); test(false); test("2"); test({}); test([]);
function optional(value?: number) { console.log(typeof value === "string", typeof value !== "string"); }
optional(); optional(3); optional(0 / 0);
let visits = 0;
function next(): any { visits++; return visits === 1 ? 3 : "x"; }
console.log(typeof next() === "string", typeof next() !== "string", visits);
function nothing(): void { visits++; }
console.log(typeof nothing() === "string", visits);

function classify(value: any): number {
    let result = 0;
    if (typeof value === "string") result += 1;
    if ("string" === typeof value) result += 2;
    if (typeof value !== "string") result += 4;
    if ("string" !== typeof value) result += 8;
    return result;
}
for (const value of [2, NaN, Infinity, -0, undefined, null, false, "2", {}, []]) console.log(classify(value));
if (typeof nothing() === "string") console.log("wrong");
else console.log("void", visits);
if ("string" !== typeof next()) console.log("text", visits);
function optionalCondition(value?: number): number {
    if (typeof value === "string") return 1;
    return 2;
}
console.log(optionalCondition(), optionalCondition(3), optionalCondition(NaN));

function looseClassify(value: any): number {
    let result = 0;
    if (typeof value == "string") result += 1;
    if ("string" == typeof value) result += 2;
    if (typeof value != "string") result += 4;
    if ("string" != typeof value) result += 8;
    console.log('loose values', typeof value == "string", "string" == typeof value,
        typeof value != "string", "string" != typeof value);
    return result;
}
for (const value of [2, NaN, Infinity, -0, undefined, null, false, "2", {}, [], 1n, Symbol()])
    console.log('loose branch', looseClassify(value));
function looseOptional(value?: number): boolean {return typeof value == "string";}
console.log('loose optional', looseOptional(), looseOptional(3), looseOptional(NaN));
console.log('loose effects', typeof next() == "string", "string" != typeof next(), visits);
console.log('loose void', typeof nothing() == "string", visits);
const looseItems: any[] = [3, 'text', , undefined, NaN];
let looseGets = 0;
Object.defineProperty(looseItems, '1', {get() {looseGets++; return 4;}, configurable: true});
for (let i=0;i<looseItems.length;i++) {
    console.log('loose array', typeof looseItems[i] == "string", "string" != typeof looseItems[i]);
}
console.log('loose getters', looseGets);

async function looseAwait(): Promise<void> {
    console.log('loose await start');
    console.log('loose await number', typeof await Promise.resolve(3) == "string");
    console.log('loose await text', "string" != typeof await Promise.resolve('text'));
    console.log('loose await missing', typeof await Promise.resolve(undefined) == "string");
}
looseAwait();
console.log('loose await scheduled');
function* looseYield(): Generator<number, boolean, any> {
    const first = typeof (yield 1) == "string";
    const second = "string" != typeof (yield 2);
    return first && second;
}
const looseIterator = looseYield();
console.log('loose yield', JSON.stringify(looseIterator.next()), JSON.stringify(looseIterator.next(3)), JSON.stringify(looseIterator.next('text')));

const stringItems: any[] = ['first', 2, , 'last'];
const stringPrototype = Object.create(Array.prototype);
let inheritedStringReads = 0;
Object.defineProperty(stringPrototype, '2', {get() {inheritedStringReads++; return 'inherited';}});
Object.setPrototypeOf(stringItems, stringPrototype);
for (let i=0;i<stringItems.length;i++) console.log('inherited string', typeof stringItems[i] === 'string');
console.log('inherited calls', inheritedStringReads);
const changedStrings: (number|string)[] = [1, 'text'];
changedStrings.reverse();
for(let i=0;i<changedStrings.length;i++)console.log('materialized string',typeof changedStrings[i] === 'string');
console.log('boxed string', typeof new String('text') === 'string');

function optionalString(value?: string): boolean {return typeof value === 'string';}
console.log('optional strings', optionalString(), optionalString(''), optionalString('text'));
const indexedStrings: any[] = ['text'];
(indexedStrings as any)[0.5] = 'fractional';
(indexedStrings as any)[-1] = 'negative';
function stringAt(items: any[], index: number): boolean {return typeof items[index] === 'string';}
console.log('named indices', stringAt(indexedStrings, 0.5), stringAt(indexedStrings, -1), stringAt(indexedStrings, NaN));
