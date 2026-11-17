const directBuffer = new ArrayBuffer(0, { maxByteLength: 8 });
const direct = new Uint8Array(directBuffer);
direct[2] = { valueOf() { directBuffer.resize(4); return 11; } } as any;
console.log(direct.length, direct[2]);
const reflectedBuffer = new ArrayBuffer(0, { maxByteLength: 8 });
const reflected = new Uint8Array(reflectedBuffer);
console.log(Reflect.set(reflected, "2", { valueOf() { reflectedBuffer.resize(4); return 12; } }), reflected.length, reflected[2]);
const fixedBuffer = new ArrayBuffer(4, { maxByteLength: 8 });
const fixed = new Uint8Array(fixedBuffer, 2, 2);
fixedBuffer.resize(0);
fixed[0] = { valueOf() { fixedBuffer.resize(4); return 13; } } as any;
console.log("fixed", fixed.length, fixed[0], fixed[1]);
const shrinkBuffer = new ArrayBuffer(4, { maxByteLength: 8 });
const shrink = new Uint8Array(shrinkBuffer);
console.log("shrink", Reflect.set(shrink, "2", { valueOf() { shrinkBuffer.resize(0); return 14; } }), shrink.length, shrink[2] === undefined);
for (const key of ["-0", "-1", "0.5", "NaN", "Infinity"]) {
 const buffer = new ArrayBuffer(0, { maxByteLength: 4 });
 const array = new Uint8Array(buffer);
 console.log(key, Reflect.set(array, key, { valueOf() { buffer.resize(4); return 15; } }), array.length, Object.hasOwn(array, key));
}
const bigBuffer = new ArrayBuffer(0, { maxByteLength: 16 });
const big = new BigInt64Array(bigBuffer);
big[1] = { valueOf() { bigBuffer.resize(16); return 3n; } } as any;
console.log("bigint", big.length, big[1] === 3n);
let conversions = 0;
const wrappedBuffer = new ArrayBuffer(0, { maxByteLength: 4 });
const wrapped = new Proxy(new Uint8Array(wrappedBuffer), {});
console.log("proxy", Reflect.set(wrapped, "0", { valueOf() { conversions++; wrappedBuffer.resize(4); return 16; } }), conversions, wrappedBuffer.byteLength);
const rejectedBuffer = new ArrayBuffer(0, { maxByteLength: 4 });
try { new Uint8Array(rejectedBuffer)[0] = { valueOf() { throw new Error("conversion"); } } as any; }
catch (error) { console.log((error as Error).message, rejectedBuffer.byteLength); }
const sharedBuffer = new SharedArrayBuffer(0, { maxByteLength: 8 });
const shared = new Uint8Array(sharedBuffer);
shared[2] = { valueOf() { sharedBuffer.grow(4); return 17; } } as any;
console.log("shared", shared.length, shared[2]);
