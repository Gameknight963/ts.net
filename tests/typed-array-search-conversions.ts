/// <reference lib="es2024.arraybuffer" />
export {};
for (const method of ['includes', 'indexOf', 'lastIndexOf'] as const) {
    for (const action of ['shrink', 'detach', 'grow']) {
        const buffer = new ArrayBuffer(4, {maxByteLength: 8});
        const values = new Uint8Array(buffer);
        values.set([1, 2, 3, 4]);
        const start = {valueOf() {
            console.log('convert', method, action);
            if (action === 'detach') buffer.transfer();
            else buffer.resize(action === 'grow' ? 8 : 1);
            return method === 'lastIndexOf' ? 3 : 0;
        }};
        console.log(method, action, (values[method] as Function)(undefined, start), values.length);
    }
}
for (const method of ['includes', 'indexOf', 'lastIndexOf'] as const) {
    const empty = new Uint8Array(0);
    const start = {valueOf() {throw new Error('empty must not convert');}};
    console.log('empty', method, (empty[method] as Function)(0, start));
    const buffer = new ArrayBuffer(2, {maxByteLength: 4});
    const fixed = new Uint8Array(buffer, 0, 2);
    fixed.set([7, 8]);
    console.log('fixed', method, (fixed[method] as Function)(undefined, {valueOf() {buffer.resize(0); return 0;}}));
}
const floats = new Float64Array([NaN, -0, 0, 4]);
console.log('same value', floats.includes(NaN), floats.indexOf(NaN), floats.lastIndexOf(NaN), floats.indexOf(0), floats.lastIndexOf(-0));
const big = new BigInt64Array([1n, 2n, 1n]);
console.log('bigint', big.includes(1n), big.indexOf(1n), big.lastIndexOf(1n), (big.includes as Function)(1));
for (const action of ['shrink', 'detach', 'grow']) {
    const buffer = new ArrayBuffer(4, {maxByteLength: 8});
    const values = new Uint8Array(buffer);
    values.set([1, 2, 3, 4]);
    console.log('at', action, (values.at as Function)({valueOf() {
        if (action === 'detach') buffer.transfer();
        else buffer.resize(action === 'grow' ? 8 : 1);
        return -1;
    }}));
}
const detachedBuffer = new ArrayBuffer(0);
const detached = new Uint8Array(detachedBuffer);
detachedBuffer.transfer();
for (const method of ['includes', 'indexOf', 'lastIndexOf', 'at'] as const) {
    try {(detached[method] as Function)(0);}
    catch (error) {console.log('detached', method, (error as Error).name);}
}
