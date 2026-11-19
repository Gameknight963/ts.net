export {};
const cases: any[] = [
    new Int8Array([-128, -1, 0, 127]), new Uint8Array([0, 1, 255]),
    new Uint8ClampedArray([-1, 0.5, 1.5, 300]), new Int16Array([-32768, -1, 255, 32767]),
    new Uint16Array([0, 255, 256, 65535]), new Int32Array([-2147483648, -1, 255, 2147483647]),
    new Uint32Array([0, 255, 256, 2147483648, 4294967295]),
    new (globalThis as any).Float16Array([-0, 0.25, NaN, Infinity]),
    new Float32Array([-0, 0.25, NaN, Infinity]), new Float64Array([-0, 0.25, NaN, Infinity]),
    new BigInt64Array([-1n, 0n, 255n]), new BigUint64Array([0n, 255n, 18446744073709551615n])
];
for (const values of cases) {
    const result: unknown[] = [];
    for (let i = 0; i < values.length; i++) result.push(values[i]);
    console.log(values.constructor.name, result.map(value => typeof value).join(','), result.join(','), Object.is(result[0], -0));
    console.log(values[-1], values[values.length], values[NaN]);
    console.log([...values.values()].join(','), [...values.entries()].map(entry => entry.join(':')).join(','));
}
const buffer = new ArrayBuffer(16);
const offset: any = new Uint8Array(buffer, 4, 4);
offset.set([1, 2, 254, 255]);
console.log('offset', offset[0], offset[3], offset[4], new Uint8Array(buffer)[4]);
