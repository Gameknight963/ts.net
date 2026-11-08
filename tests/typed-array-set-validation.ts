/// <reference lib="es2024.arraybuffer" />
export {};
for (const offset of [-1, 3, Infinity]) {
    for (const size of [0, 1]) {
        const target = new Uint8Array(2);
        const source = new BigInt64Array(size);
        try {target.set(source as any, offset); console.log('unexpected');}
        catch (error) {console.log('mismatch', offset, size, (error as Error).name);}
    }
    const source = new Uint8Array(1);
    source.buffer.transfer();
    try {new Uint8Array(2).set(source, offset); console.log('unexpected');}
    catch (error) {console.log('detached', offset, (error as Error).name);}
}
const target = new Uint8Array(1);
let order = '';
const source = {get length() {order += 'length|'; return 0;}};
try {target.set(source, {valueOf() {order += 'offset|'; target.buffer.transfer(); return -1;}} as any);}
catch (error) {console.log('negative first', order, (error as Error).name);}
const numeric = new Uint8Array(3);
numeric.set('123' as any);
const big = new BigInt64Array(3);
big.set('456' as any);
console.log('strings', numeric.join(','), big.join(','));
