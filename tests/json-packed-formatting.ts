export {};
const values: number[] = [0, 0.5];
const buffer = new ArrayBuffer(8), view = new DataView(buffer);
let state = 123456789;
while (values.length < 2000) {
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0;
    view.setUint32(0, state, true);
    state = (Math.imul(state, 1664525) + 1013904223) >>> 0;
    view.setUint32(4, state, true);
    const value = view.getFloat64(0, true), magnitude = Math.abs(value);
    if (magnitude >= 1e-4 && magnitude < 1e15) values.push(value);
}
console.log(JSON.stringify(values));
console.log(JSON.stringify({values}));
console.log(JSON.stringify(values, null, 2));
for (const value of [0, -0, NaN, Infinity, -Infinity, 1e-4, -1e-4, 0.00009999999999999999, 1e15, 999999999999999.9, 1e21, 1e-7]) {
    const changed = values.slice(); changed[127] = value;
    console.log(JSON.stringify(changed));
}
const copy = values.slice(); copy.length = 2001;
console.log(JSON.stringify(copy));
console.log(JSON.stringify(values, ['0', '1']));
let replacerCalls = 0;
console.log(JSON.stringify(values, (key, value) => {replacerCalls++; return value;}), replacerCalls);
let toJSONCalls = 0;
const root: any = {toJSON() {toJSONCalls++; return values;}};
console.log(JSON.stringify(root), toJSONCalls);
const custom = values.slice();
(custom as any).toJSON = () => ({changed: true});
console.log(JSON.stringify(custom));
let reads = 0;
Object.defineProperty(values, '7', {get() {reads++; return 17.5;}});
console.log(JSON.stringify(values), reads);
