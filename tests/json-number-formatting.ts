export {};
const values = [1, -1, 255, 256, Number.MIN_SAFE_INTEGER, Number.MAX_SAFE_INTEGER, 0, -0, NaN, Infinity, -Infinity, 1e-7, 1e-6, 1e20, 1e21,
    -1e-7, -1e-6, -1e20, -1e21, Number.MIN_VALUE, Number.MAX_VALUE,
    2147483647, 2147483648, -2147483648, -2147483649, 1.2345678901234567,
    1.2345678901234567e-6, 1.2345678901234567e20];
console.log(JSON.stringify(values));
console.log(JSON.stringify({values, tiny: Number.MIN_VALUE, large: Number.MAX_VALUE}, null, 2));
const buffer = new ArrayBuffer(8), view = new DataView(buffer);
let state = 123456789;
for (let i = 0; i < 1000; i++) {
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0;
    view.setUint32(0, state, true);
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0;
    view.setUint32(4, state, true);
    values.push(view.getFloat64(0, true));
}
console.log(JSON.stringify(values));
console.log(JSON.stringify({nested: values.slice(20, 40)}, null, '  '));

for (const value of values.slice(0, 30)) {
    console.log(JSON.stringify([value]), JSON.stringify({value}));
    console.log(JSON.stringify({value}, (_key, item) => item));
}
const rows = Array.from({length: 8}, (_, i) => ({a: values[i], b: values[i + 8],
    c: values[i + 16], d: values[i + 24], e: i, f: -i}));
console.log(JSON.stringify(rows));
