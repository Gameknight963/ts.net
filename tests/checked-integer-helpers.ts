export {};
function read(values: any[], index: number): any { return values[index]; }
const values: any[] = [7, "text", 9];
let sum = 0;
for (let index = 0; index < 8; index++) sum += read(values, (index & 1) << 1) as number;
console.log("loop", sum);
(values as any)["-1"] = 11;
(values as any)["4294967295"] = 13;
(values as any)["-2147483648"] = 17;
(values as any)["2147483648"] = 19;
console.log("negative", (read(values, ~0) as number) + 0);
console.log("signed", (read(values, 0x80000000 | 0) as number) + 0);
console.log("unsigned", (read(values, 0xFFFFFFFF >>> 0) as number) + 0, (read(values, 0x80000000 >>> 0) as number) + 0);
for (const value of [undefined, NaN, -0, Infinity, -Infinity]) {
    values[0] = value;
    console.log("value", 1 / (read(values, 0) as number));
}
let order = "";
function array(): any[] { order += "a"; return values; }
function index(): number { order += "i"; return 1; }
console.log("order", (read(array(), (index() & 1) << 1) as number) + 0, order);
let gets = 0;
Object.defineProperty(values, "2", { configurable: true, get() { gets++; return 23; } });
console.log("getter", (read(values, 1 << 1) as number) + 0, gets);
delete values[2];
const prototype = Object.create(Array.prototype);
prototype[2] = 29;
Object.setPrototypeOf(values, prototype);
console.log("inherited", (read(values, 2) as number) + 0);
const numeric = [31, 37];
console.log("numeric", (read(numeric, 1) as number) + 0);
try { console.log((read(null as any, 0) as number) + 0); } catch (error) { console.log("null", (error as Error).name); }

const dense: any[] = [41, "text", 43];
Object.defineProperty(dense, "0", {value: 47, writable: false, configurable: true});
console.log("defined data", (read(dense, 0) as number) + 0);
dense.length = 1;
console.log("truncated", (read(dense, 2) as number) + 0);
dense[2] = 53;
console.log("regrown", (read(dense, 2) as number) + 0);
const reversed: any[] = [59, "text", 61];
reversed.reverse();
console.log("reversed", (read(reversed, 0) as number) + 0);
Object.freeze(reversed);
console.log("frozen", (read(reversed, 2) as number) + 0);
