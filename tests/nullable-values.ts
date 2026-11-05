function numberValue(defined: boolean): number | undefined { return defined ? 0 : undefined; }
function booleanValue(defined: boolean): boolean | undefined { return defined ? false : undefined; }
function numberEcho(value: number) { return value; }
function booleanEcho(value: boolean) { return value; }
function numberOptional(value?: number) { return value; }
function booleanOptional(value?: boolean) { return value; }
function implicitNumber(flag: boolean) { if (flag) return 3; }
function implicitBoolean(flag: boolean) { if (flag) return false; }
function bareReturn(flag: boolean): number | undefined { if (flag) return 1; return; }
function overwriteNumber(value: any) { let local: number = 42; local = value; return local; }
function overwriteBoolean(value: any) { let local: boolean = true; local = value; return local; }
const absentNumber = numberValue(false);
const absentBoolean = booleanValue(false);
console.log(absentNumber, typeof absentNumber, absentNumber === undefined, absentNumber ?? 7);
console.log(numberValue(true), numberValue(true) === undefined, numberValue(true) ?? 7);
console.log(absentBoolean, typeof absentBoolean, absentBoolean === undefined, absentBoolean ?? true);
console.log(booleanValue(true), booleanValue(true) === undefined, booleanValue(true) ?? true);
console.log(numberOptional(), booleanOptional(), numberOptional(0), booleanOptional(false));
console.log(implicitNumber(false), implicitNumber(true), implicitBoolean(false), implicitBoolean(true), bareReturn(false));

const erased: any = undefined;
console.log(overwriteNumber(erased), overwriteBoolean(erased), overwriteNumber(0), overwriteBoolean(false));
const typedNumber: number = erased;
const typedBoolean: boolean = erased;
console.log(numberEcho(typedNumber), booleanEcho(typedBoolean), typeof numberEcho(typedNumber));
console.log(typedNumber === undefined, typedBoolean === undefined);
console.log(typedNumber + 1, typedNumber * 0, -typedNumber, +typedNumber);
const arithmetic = typedNumber + 1;
console.log(arithmetic === undefined, arithmetic !== arithmetic, typeof arithmetic);
console.log(!typedBoolean, typedBoolean && true, typedBoolean || false);
let updated: number = erased;
console.log(updated++, updated, updated === undefined);
let assigned: number;
assigned = typedNumber;
console.log(assigned === undefined, numberEcho(assigned));

type Values = { n: number | undefined; b: boolean | undefined };
const values: Values = { n: absentNumber, b: absentBoolean };
console.log(values.n, values.b, values.n === undefined, values.b === undefined);
values.n = 0;
values.b = false;
console.log(values.n, values.b);
const dynamic: any = values;
dynamic.n = undefined;
dynamic.b = undefined;
console.log(values.n, values.b, dynamic.n === undefined, dynamic.b === undefined);
dynamic.n = 5;
dynamic.b = true;
console.log(values.n + 2, !values.b, dynamic.n, dynamic.b);
function three(value: number | null | undefined) { return value; }
console.log(three(null) === null, three(undefined) === undefined, three(0) === 0);
const nan = 0 / 0;
console.log(nan === undefined, nan !== nan, nan ?? 9, -0);
type Optional = { n?: number; b?: boolean };
const optional: Optional = {};
console.log(optional.n, optional.b, optional.n === undefined, optional.b === undefined);
optional.n = 0;
optional.b = false;
console.log(optional.n, optional.b);
const optionalDynamic: any = optional;
optionalDynamic.n = undefined;
optionalDynamic.b = undefined;
console.log(optional.n, optional.b);
