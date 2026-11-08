for (const type of [Uint8Array, Int16Array, Float32Array, Float64Array]) {
    const source = new type([1,2,3]);
    (source as any)[Symbol.iterator] = () => { throw 'iterator must not run'; };
    const copied = new type(source);
    source[0] = 99;
    console.log(type.name, copied.join(','), copied.buffer !== source.buffer);
}
const offset = new Int16Array(new ArrayBuffer(16), 4, 3);
offset.set([-1,256,32767]);
console.log('offset', new Uint8Array(offset).join(','), new Int16Array(offset).join(','));
const big = new BigInt64Array([-1n,2n]);
(big as any)[Symbol.iterator] = () => { throw 'iterator must not run'; };
console.log('big', new BigUint64Array(big).join(','));
for (const size of [0,1]) {
    try { new Uint8Array(new BigInt64Array(size) as any); }
    catch (error) { console.log('number mismatch', size, (error as Error).name); }
    try { new BigInt64Array(new Uint8Array(size) as any); }
    catch (error) { console.log('big mismatch', size, (error as Error).name); }
}
const nanBuffer = new ArrayBuffer(16);
const bits = new DataView(nanBuffer);
bits.setBigUint64(0, 0x7ff8000000001234n, true);
bits.setBigUint64(8, 0x8000000000000000n, true);
const floats = new Float64Array(nanBuffer);
const copiedFloats = new Float64Array(floats);
const copiedBits = new DataView(copiedFloats.buffer);
console.log('bits', copiedBits.getBigUint64(0,true).toString(16), copiedBits.getBigUint64(8,true).toString(16));
const detached = new Uint8Array([1]);
structuredClone(detached.buffer, { transfer: [detached.buffer] });
try { new Uint8Array(detached); }
catch (error) { console.log('detached', (error as Error).name); }
