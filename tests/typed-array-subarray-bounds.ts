/// <reference lib="es2024.arraybuffer" />
export {};
for (const offset of [0, 6]) {
    const buffer = new ArrayBuffer(8, {maxByteLength: 16});
    const source = new Uint8Array(buffer, offset, 8 - offset);
    buffer.resize(4);
    try {const view = source.subarray(0); console.log('out of bounds', offset, view.length, view.byteOffset);}
    catch (error) {console.log('out of bounds error', offset, (error as Error).name);}
}
for (const tracking of [false, true]) {
    const buffer = new ArrayBuffer(8, {maxByteLength: 16});
    const source = tracking ? new Uint8Array(buffer) : new Uint8Array(buffer, 0, 8);
    const start = {valueOf() {buffer.resize(4); return 0;}};
    try {const view = source.subarray(start as any); console.log('shrink', tracking, view.length, view.byteOffset);}
    catch (error) {console.log('shrink error', tracking, (error as Error).name);}
}
const detached = new Uint8Array(4);
detached.buffer.transfer();
let calls = 0;
Object.defineProperty(detached, 'constructor', {value: {[Symbol.species]: function() {calls++; return new Uint8Array([9]);}}});
try {console.log('custom detached', detached.subarray(0).join(','), calls);}
catch (error) {console.log('custom detached error', (error as Error).name, calls);}
const mismatch = new BigInt64Array(0);
Object.defineProperty(mismatch, 'constructor', {value: {[Symbol.species]: function() {return new Uint8Array(0);}}});
console.log('custom content', mismatch.subarray(0) instanceof Uint8Array);
