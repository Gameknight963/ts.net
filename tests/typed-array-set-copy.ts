const data = new Uint8Array([1,2,3,4,5]);
data.set(data.subarray(0,4),1);
console.log('forward', data.join(','));
data.set(data.subarray(1),0);
console.log('backward', data.join(','));
const buffer = new ArrayBuffer(12);
const bytes = new Uint8Array(buffer); bytes.set([1,2,3,4,5,6]);
const wide = new Uint16Array(buffer,0,3);
const expected = Array.from(wide);
bytes.set(wide,1);
console.log('cross width', bytes.slice(1,4).join(','), expected.map(value => value & 255).join(','));
class HiddenLength extends Uint8Array { get length(): number { throw 'length getter must not run'; } }
const hidden = new HiddenLength([7,8]);
const target = new Uint8Array(3);
target.set(hidden,1);
console.log('length override', target.join(','));
for (const size of [0,1]) {
    try { new Uint8Array(2).set(new BigInt64Array(size) as any); }
    catch(error) { console.log('number mismatch', size, (error as Error).name); }
    try { new BigInt64Array(2).set(new Uint8Array(size) as any); }
    catch(error) { console.log('big mismatch', size, (error as Error).name); }
}
const source = new Float64Array(2);
const bits = new DataView(source.buffer);
bits.setBigUint64(0,0x7ff8000000001234n,true);
bits.setBigUint64(8,0x8000000000000000n,true);
const copied = new Float64Array(3);copied.set(source,1);
const output = new DataView(copied.buffer);
console.log('bits',output.getBigUint64(8,true).toString(16),output.getBigUint64(16,true).toString(16));
const detachable = new Uint8Array(0);
try { detachable.set([], {valueOf() { structuredClone(detachable.buffer,{transfer:[detachable.buffer]}); return 0; }} as any); }
catch(error) { console.log('detached offset', (error as Error).name); }
