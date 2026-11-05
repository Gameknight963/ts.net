function twice(n: number): number { return n * 2; }
function add(first = 2, second = 3): number { return first + second; }
function apply(f: (n: number) => number, n: number): number { return f(n); }
function apply2(f: (a: number, b: number) => number, a: number, b: number): number { return f(a, b); }
function apply0(f: () => number): number { return f(); }
const inc = (n: number) => n + 1;
console.log(inc(3), apply(inc, 3), apply(twice, 4));
console.log(typeof inc, inc.name, inc.length, twice.name, twice === twice);
console.log(apply2((a, b) => a + b, 3, 4), apply0(() => 9));
const optional = (n = 5) => n + 1;
console.log(apply0(optional), apply(optional, 8), optional.length);
function defaults(f: (n: number) => number = twice, n = 4) { return f(n); }
console.log(defaults(), defaults(inc, 3));
const generic: any = add;
console.log(generic(), generic(5), generic(5, 8), generic(...[5, 8, 99]));
console.log(generic.name, generic.length, typeof generic);
function sum(...values: number[]) { let total = 0; for (const n of values) total += n; return total; }
const rest: any = sum;
console.log(rest(1, 2, 3), rest(...[1, 2, 3]), rest(...[]), rest.length);
function unpack({ x, y }: { x: number; y: number }) { return x + y; }
const destructured: any = unpack;
console.log(destructured({ x: 3, y: 4 }));
const objectArrow = ({ x, y }: { x: number; y: number }) => x + y;
console.log(objectArrow({ x: 5, y: 6 }));
const holder = { run: twice };
console.log(holder.run(9));
const choices = [inc, twice];
console.log(choices[0](3), choices[1](3));
let selected = inc;
selected = twice;
console.log(selected(3));
const original = selected;
function replace() { selected = inc; return 4; }
console.log(selected(replace()), selected(4), original(4));
selected = twice;
console.log(selected(...[replace()] as [number]), selected(4));
function maker() { return (n: number) => n + 1; }
const first = maker();
const second = maker();
console.log(first === second, first(3), first.name, first.length);
const anonymous = function (n: number) { return n * 3; };
console.log(anonymous(4), anonymous.name, anonymous.length);
const factorial = function fact(n: number): number { return n <= 1 ? 1 : n * fact(n - 1); };
console.log(factorial(5), factorial.name);
function sameFunction(): typeof sameFunction { return sameFunction; }
console.log(sameFunction() === sameFunction);
const same = inc;
console.log(same === inc, same(4));
const decorated: any = twice;
decorated.extra = 7;
console.log((twice as any).extra, Object.hasOwn(decorated, "name"), Object.hasOwn(decorated, "extra"));
const text = (value: string) => value + "!";
console.log(text("hello"));
function applyText(f: (value: string) => string, value: string) { return f(value); }
console.log(applyText(text, "world"));
const yes = (value: boolean) => !value;
function applyBool(f: (value: boolean) => boolean, value: boolean) { return f(value); }
console.log(applyBool(yes, false));
let output: any = "";
function log(value: any): void { output = value; }
function run(f: () => void) { f(); return 1; }
console.log(run(() => log("done")), output);
const voidResult: () => void = () => 42;
voidResult();
function identity<T>(value: T): T { return value; }
const genericIdentity = identity;
console.log(genericIdentity(8), genericIdentity("generic"));
let global = 3;
const readGlobal = () => global;
global = 4;
console.log(readGlobal());
function local() {
    const f = (n: number) => n + 1;
    return f(5);
}
console.log(local(), ((n: number) => n + 1)(7));

// Numeric callback entry points must preserve missing and extra arguments.
const constantNumber = () => 7;
const plus = (a: number, b: number) => a + b;
const numericIdentity = (n: number) => n;
console.log(apply(constantNumber, 3), apply2(constantNumber, 3, 4));
console.log(apply0(twice as unknown as () => number));
console.log(apply0(plus as unknown as () => number));
console.log(apply(plus as unknown as (n: number) => number, 3));
console.log(apply2(twice, 3, 4));
console.log(apply0(numericIdentity as unknown as () => number));
console.log(apply2(numericIdentity, 3, 4));
