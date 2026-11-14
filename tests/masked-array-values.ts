export {};
function masked(index: number): number { const values = [3, 7]; return values[index & 1] + values[1]; }
for (const index of [-Infinity, -4294967297, -1.5, -1, -0, 0, 1, 2, 4294967297, Infinity, NaN, undefined])
    console.log("index", index, masked(index as number));
let calls = 0;
function next(): number { return calls++; }
function effects(): number { const values = [3, 7]; return values[next() & 1] + values[next() & 1]; }
console.log("effects", effects(), calls);
function missing(): number { return undefined as any; }
function optional(index: number): boolean { const values = [missing(), 7]; return values[index & 1] === undefined; }
console.log("missing", optional(0), optional(1));
function zero(index: number): boolean { const values = [0, -0]; return Object.is(values[index & 1] + -0, -0); }
console.log("zero", zero(0), zero(1));
function captured(): number { const values = [1, 2]; const key = () => { values[0] = 7; return 0; }; return values[key() & 1] + values[1]; }
function alias(): number { const values = [1, 2]; const other = values; other[1] = 7; return values[1 & 1] + values[0]; }
function larger(index: number): number { const values = [1, 2, 3]; return values[index & 1] + values[2]; }
console.log("fallbacks", captured(), alias(), larger(1));
let reads = 0;
Object.defineProperty(Array.prototype, "2", { configurable: true, get() { reads++; this[0] = 4; return 9; } });
function otherMask(index: number): number { const values = [1, 2]; return values[index & 2] + values[0]; }
const result = otherMask(2);
delete (Array.prototype as any)[2];
console.log("other mask", result, reads);
