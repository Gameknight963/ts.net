/// <reference lib="es2024.arraybuffer" />
export {};
for (const empty of [false, true]) {
    const buffer = new ArrayBuffer(3), array = new Uint8Array(buffer);
    let order = '';
    const target = {valueOf() {order += 'target|'; buffer.transfer(); return 0;}};
    const start = {valueOf() {order += 'start|'; return 0;}};
    const end = {valueOf() {order += 'end|'; return empty ? 0 : 3;}};
    try {array.copyWithin(target as any, start as any, end as any); console.log('detached accepted', empty, order);}
    catch (error) {console.log('detached', empty, order, (error as Error).name);}
}
for (const tracking of [false, true]) {
    const buffer = new ArrayBuffer(4, {maxByteLength: 8});
    const array = tracking ? new Uint8Array(buffer) : new Uint8Array(buffer, 0, 4);
    array.set([1, 2, 3, 4]);
    const target = {valueOf() {buffer.resize(2); return 1;}};
    let returned = false;
    try {array.copyWithin(target as any, 0); returned = true; console.log('shrink', tracking, array.join(','));}
    catch (error) {console.log('shrink error', tracking, (error as Error).name, returned);}
}
const buffer = new ArrayBuffer(24), floats = new Float64Array(buffer), view = new DataView(buffer);
const little = new Uint8Array(new Uint16Array([1]).buffer)[0] === 1;
view.setBigUint64(0, 0x7ff8000000000001n, little);
view.setBigUint64(8, 0x7ff8000000000002n, little);
floats.copyWithin(1, 0, 2);
console.log('payloads', view.getBigUint64(8, little).toString(16), view.getBigUint64(16, little).toString(16));
const big = new BigInt64Array([1n, 2n, 3n]);
console.log('big', big.copyWithin(1, 0, 2) === big, big.join(','));
const backing = new Uint16Array([9, 1, 2, 3, 9]);
const offset = new Uint16Array(backing.buffer, 2, 3);
offset.copyWithin(1, 0, 2);
console.log('offset', backing.join(','));
offset.copyWithin(0, 1, 3);
console.log('backward', backing.join(','));
const growing = new ArrayBuffer(3, {maxByteLength: 6}), tracked = new Uint8Array(growing);
tracked.set([1, 2, 3]);
tracked.copyWithin({valueOf() {growing.resize(6); return 1;}} as any, 0);
console.log('grow', tracked.join(','));
