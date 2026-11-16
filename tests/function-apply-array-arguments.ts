export {};
const fn = (value: number): number => value;
function handler(value: Function): void {
    Object.defineProperty(fn, "apply", { configurable: true, writable: true, value });
}
handler((target: any, values: number[]): number => target.base + values[0]);
console.log("numeric", fn.apply({ base: 2 } as any, [3]));
let reads = 0;
console.log("getter", fn.apply({ get base() { reads++; return 5; } } as any, [7]), reads);
handler((_target: any, values: number[]): number => values[0]);
for (const value of [undefined, -0, NaN, Infinity, -Infinity, 11]) {
    const result = fn.apply(null, [value as number]);
    console.log("value", result, Object.is(result, -0), result === undefined);
}
handler((_target: any, values: number[]): number => values[0] + values[0]);
let evaluations = 0;
function argument(): number { evaluations++; return 13; }
console.log("repeated", fn.apply(null, [argument()]), evaluations);
handler((_target: any, values: number[]): number => typeof values[0] === "undefined" ? 17 : values[0]);
console.log("conditional", fn.apply(null, [undefined as unknown as number]), fn.apply(null, [19]));

handler((_target: any, values: number[]): number => values.length);
console.log("length only", fn.apply(null, [undefined as unknown as number]));
console.log("length argument", fn.apply(null, [argument()]), evaluations);
function failArgument(): number { throw new Error("argument"); }
try { fn.apply(null, [failArgument()]); } catch (error) { console.log("length failure", (error as Error).message); }
const lengthFunction: any = fn;
console.log("ordinary lengths", lengthFunction.apply(null, []), lengthFunction.apply(null, [1, 2]));
handler((_target: any, values: number[]): number => values.length + values[0]);
console.log("length", fn.apply(null, [31]));
handler((target: {length: number}, values: number[]): number => target.length + values.length + values[0]);
console.log("separate length", fn.apply({length: 7} as any, [3]));

// Observable arrays and argument lists keep the ordinary entry point.
let observed: number[] = [];
handler((_target: any, values: number[]): number => (observed = values, values[0]));
fn.apply(null, [23]);
const first = observed;
fn.apply(null, [29]);
console.log("escape", first !== observed, Array.isArray(observed), first[0], observed[0], Object.hasOwn(observed, "0"));
handler((_target: any, values: number[]): number => values[1] ?? 37);
console.log("other index", fn.apply(null, [41]));
handler((_target: any, values: number[]): number => ++values[0]);
console.log("mutation", fn.apply(null, [43]));
handler((_target: any, values: number[]): number => ([values[0]] = [47], values[0]));
console.log("destructuring", fn.apply(null, [0]));
handler((_target: any, values: number[]): number => (delete values[0], values[0] ?? 49));
console.log("delete", fn.apply(null, [0]));
handler((_target: any, values: number[]): number => (values.length = 0, values[0] ?? 51));
console.log("resize", fn.apply(null, [0]));
handler((_target: any, values: number[]): number => (++values.length, values.length));
console.log("grow", fn.apply(null, [0]));
handler((_target: any, values: number[]): number => (() => values[0])());
console.log("capture", fn.apply(null, [53]));
handler(function(_target: any, values: number[]): number { return arguments.length + values[0]; });
console.log("arguments", fn.apply(null, [59]));
handler((target: any = {base: 61}, values: number[]): number => target.base + values[0]);
console.log("default", fn.apply(undefined, [67]));
(fn as any).tag = 71;
handler(function(this: any, _target: any, values: number[]): number { return this.tag + values[0]; });
console.log("receiver", fn.apply(null, [73]));

handler((_target: any, values: number[]): number => values[0]);
const loose: any = fn;
console.log("ordinary calls", loose.apply(null, []), loose.apply(null, [79, 83]));
handler((_target: any, values: number[] = [89]): number => values[0]);
console.log("default array", loose.apply(null, undefined));
