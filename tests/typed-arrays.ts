const bytes = new Uint8Array([0, 1, 255, 256, -1, 3.8]);
console.log(bytes.length, bytes.byteLength, bytes.byteOffset, bytes.BYTES_PER_ELEMENT, Uint8Array.BYTES_PER_ELEMENT);
console.log(bytes.join(','), ArrayBuffer.isView(bytes), Array.isArray(bytes));
bytes[1] = 300;
console.log(bytes[1], bytes[99]);
const shared = new Uint8Array(bytes.buffer, 1, 3);
shared[0] = 9;
console.log(bytes.join(','), shared.join(','));
const view = new DataView(bytes.buffer);
view.setUint8(2, 88);
console.log(bytes[2]);
const copy = new Uint8Array(bytes);
copy[0] = 77;
console.log(bytes[0], copy[0]);
console.log(new Int8Array([128, 255, -129]).join(','));
console.log(new Uint8ClampedArray([-1, 0.5, 1.5, 2.5, 254.5, 999, NaN]).join(','));
console.log(new Int16Array([65535, 32768, 65536]).join(','));
console.log(new Uint16Array([-1, 65537]).join(','));
console.log(new Int32Array([4294967295, 2147483648]).join(','));
console.log(new Uint32Array([-1, 4294967297]).join(','));
console.log(new Float32Array([1.25, -3.5]).join(','));
console.log(new Float64Array([1.25, -3.5]).join(','));
console.log(new BigInt64Array([9223372036854775808n, -1n]).join(','));
console.log(new BigUint64Array([-1n, 18446744073709551616n]).join(','));
let total = 0;
for (const value of bytes) total += value;
console.log(total, [...bytes].join(','));
console.log([...bytes.keys()].join(','), [...bytes.entries()].map(p => p.join(':')).join(','));
const sub = bytes.subarray(1, -1);
sub[0] = 42;
console.log(bytes[1], sub.length, sub.buffer === bytes.buffer);
const sliced = bytes.slice(1, 3);
sliced[0] = 11;
console.log(bytes[1], sliced.join(','), sliced.buffer === bytes.buffer);
const overlap = new Uint8Array([1, 2, 3, 4, 5]);
overlap.set(overlap.subarray(0, 4), 1);
console.log(overlap.join(','));
overlap.set([9, 8], 0);
console.log(overlap.join(','));
console.log(overlap.fill(7, 1, 3) === overlap, overlap.join(','));
console.log(overlap.reverse() === overlap, overlap.join(','));
overlap.copyWithin(0, 3);
console.log(overlap.join(','));
console.log(overlap.map(x => x * 2).join(','), overlap.filter(x => x > 7).join(','));
console.log(overlap.map(x => x * 100) instanceof Uint8Array);
console.log(overlap.reduce((a, b) => a + b, 0), overlap.some(x => x === 9), overlap.every(x => x > 0));
console.log(overlap.find(x => x > 7), overlap.findIndex(x => x === 7), overlap.includes(9), overlap.indexOf(9));
console.log(Uint16Array.of(1, 65537).join(','), Uint8Array.from([1, 2, 3], (x, i) => x + i).join(','));
console.log(new Uint8Array({0: 7, 1: 8, length: 2}).join(','));
console.log(Object.keys(new Uint8Array([2, 3])).join(','));
class PacketBytes extends Uint8Array { sum() { return this.reduce((a, b) => a + b, 0); } }
const packet = new PacketBytes([4, 5]);
console.log(packet.sum(), packet instanceof PacketBytes, packet instanceof Uint8Array);
for (const operation of [() => new Uint16Array(new ArrayBuffer(3)), () => new Uint16Array(new ArrayBuffer(8), 1), () => new Uint8Array(new ArrayBuffer(2), 0, 3), () => bytes.set([1, 2], 5)]) {
    try { operation(); } catch (error) { console.log(error instanceof RangeError); }
}
try { new BigInt64Array([1] as any); } catch (error) { console.log(error instanceof TypeError); }

const sorted = new Float64Array([10, 2, NaN, -0, 0, -3]);
console.log(sorted.sort() === sorted, sorted.join(','), Object.is(sorted[1], -0));
console.log(new Uint8Array([10, 2, 1]).sort((a, b) => b - a).join(','));
console.log(new BigInt64Array([10n, 2n, -1n]).sort().join(','));
const newer = bytes as any;
console.log(newer.toReversed().join(','), newer.with(0, 999).join(','), newer.toSorted().join(','));
console.log(bytes.join(','));

const Bytes = Uint8Array;
console.log(new Bytes([3, 4]).join(','));
let floating = new Float32Array(2);
floating[0] = -0;
floating[1] = NaN;
console.log(Object.is(floating[0], -0), Number.isNaN(floating[1]));
const sourceLike = { length: 3, 0: 1, 2: 3 };
console.log(new Uint8Array(sourceLike).join(','));
console.log(new Uint8Array([1, 2, 3]).at(-1));

console.log(bytes[99] + 1, bytes[-1], bytes[0.5]);
const mapped = new Uint8Array([1, 2]).map(x => x * 200);
console.log(mapped.join(','));
const iterated = new Uint8Array(new Set([3, 2, 3, 1]));
console.log(iterated.join(','));
const anyBytes = bytes as any;
anyBytes[-1] = 4;
anyBytes['01'] = 5;
console.log(anyBytes[-1], anyBytes['01'], bytes[1]);
