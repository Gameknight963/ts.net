function identity(value: number): number { return value; }
const direct: any = identity;
console.log(direct(), direct(...[]), direct(3), direct(3, 4));
const indirect: any = (identity as unknown);
console.log(indirect(), indirect(3, 4));
function describe(value: { x: number }): string { return typeof value; }
const describeAny: any = describe;
console.log(describeAny());
function types(number: number, flag: boolean, text: string): string {
    return typeof number + ":" + typeof flag + ":" + typeof text;
}
const typeAny: any = types;
console.log(typeAny(), typeAny(1), typeAny(1, false));
let trace = "";
function mark(value: number): number { trace += value;return value; }
function markVoid(value: number): void { trace += value; }
console.log(direct(mark(1), mark(2), markVoid(3)), trace);
trace = "";
console.log(indirect(mark(1), mark(2), markVoid(3)), trace);
function defaults(first: any = 5, second: any = 7): any { return [first, second]; }
const defaultAny: any = defaults;
const missing = defaultAny();
const explicit = defaultAny(null);
console.log(missing[0], missing[1], explicit[0], explicit[1]);
function rest(first: number, ...values: number[]): number { return typeof first === "undefined" ? -1 : first + values.length; }
const restAny: any = rest;
console.log(restAny(), restAny(3), restAny(3, 4, 5));
function noArgs(): number { return 7; }
const noArgsAny: any = noArgs;
trace = "";
console.log(noArgsAny(mark(4), markVoid(5)), trace);
