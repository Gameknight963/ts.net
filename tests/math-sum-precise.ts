export {};
// Node versions without sumPrecise use this exact-integer oracle. Each input
// is decoded separately; the runtime instead accumulates fixed-width bins.
function reference(items: Iterable<number>): number {
 let total = 0n, negativeZero = true, positive = false, negative = false, nan = false;
 const view = new DataView(new ArrayBuffer(8));
 for (const value of items) {
  if (typeof value !== "number") throw new TypeError("number required");
  if (Number.isNaN(value)) { nan = true; continue; }
  if (value === Infinity) { positive = true; continue; }
  if (value === -Infinity) { negative = true; continue; }
  if (!Object.is(value, -0)) negativeZero = false;
  view.setFloat64(0, value);
  const bits = view.getBigUint64(0), exponent = Number((bits >> 52n) & 2047n);
  const mantissa = (bits & ((1n << 52n) - 1n)) | (exponent ? 1n << 52n : 0n);
  const term = mantissa << BigInt(Math.max(0, exponent - 1));
  total += bits >> 63n ? -term : term;
 }
 if (nan || positive && negative) return NaN;
 if (positive) return Infinity;
 if (negative) return -Infinity;
 if (negativeZero) return -0;
 if (total === 0n) return 0;
 const sign = total < 0n ? -1 : 1;
 if (total < 0n) total = -total;
 let shift = 0n;
 while (total >> shift >= 1n << 53n) shift++;
 let rounded = total >> shift;
 if (shift) {
  const rest = total - (rounded << shift), half = 1n << (shift - 1n);
  if (rest > half || rest === half && (rounded & 1n) !== 0n) rounded++;
 }
 return sign * (Number(rounded) * 2 ** (Number(shift) - 1074));
}
const sum: (items: Iterable<number>) => number = (Math as any).sumPrecise ?? reference;
const cases: [number[], number][] = [
 [[], -0], [[-0, -0], -0], [[-0, 0], 0], [[1, -1], 0],
 [[1e20, 1, -1e20], 1], [[1, 2 ** -53], 1], [[1, 2 ** -52, 2 ** -53], 1 + 2 ** -51],
 [[Number.MAX_VALUE, Number.MAX_VALUE, -Number.MAX_VALUE], Number.MAX_VALUE],
 [[Number.MAX_VALUE, Number.MAX_VALUE], Infinity],
 [[Number.MIN_VALUE, Number.MIN_VALUE], Number.MIN_VALUE * 2],
 [[Infinity, -Infinity], NaN], [[NaN, 1], NaN], [[-Infinity, 1], -Infinity]
];
for (const [input, expected] of cases) {
 if (!Object.is(sum(input), expected)) throw new Error("sum case " + input.join(","));
}
let state = 12345;
function random(): number { state = (Math.imul(state, 1664525) + 1013904223) | 0; return state >>> 0; }
const bits = new DataView(new ArrayBuffer(8));
for (let trial = 0; trial < 300; trial++) {
 const values: number[] = [];
 for (let i = 0; i < 30; i++) {
  bits.setUint32(0, random()); bits.setUint32(4, random());
  const value = bits.getFloat64(0);
  if (Number.isFinite(value)) { values.push(value); if (i % 3 === 0) values.push(-value); }
 }
 if (!Object.is(sum(values), reference(values))) throw new Error("random sum " + trial);
}
console.log("precise sums", cases.length, 300);
console.log("iterables", sum(new Set([1, 2, 3])), sum(new Float64Array([1e20, 1, -1e20])));
let closed = 0;
function* invalid(): Generator<any> { try { yield NaN; yield "1"; } finally { closed++; } }
try { sum(invalid()); } catch (error) { console.log("invalid", (error as Error).name, closed); }
for (const input of [[1n], [new Number(1)], [undefined], [null]]) {
 try { sum(input as any); } catch (error) { console.log("strict", (error as Error).name); }
}
for (const input of [null, undefined, 1, {}, {next() {return {done: true};}}, {[Symbol.iterator]: 1}, {[Symbol.iterator]() {return 1;}}]) {
 try { sum(input as any); console.log("unexpected iterable"); } catch (error) { console.log("iterable error", (error as Error).name); }
}
const noIterator: any = [1, 2]; noIterator[Symbol.iterator] = null;
try { sum(noIterator); } catch (error) { console.log("disabled iterator", (error as Error).name); }
let effects = "";
const custom = {
 get [Symbol.iterator]() { effects += "I"; return function() {
  let index = 0;
  return {get next() {effects += "N"; return function(): IteratorResult<number> {effects += "S"; return index++ ? {done: true, value: undefined} : {value: 3, done: false};};}};
 };}
};
console.log("protocol", sum(custom), effects);
let returns = 0;
const throwing = {[Symbol.iterator]() {return {next() {throw new Error("step");}, return() {returns++; return {};}};}};
try { sum(throwing as any); } catch (error) { console.log("step error", (error as Error).message, returns); }
