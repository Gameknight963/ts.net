export {};
function make(value: number): { value: number } { return { value }; }
function read(target: any): any { return target.value; }
const values: number[] = [0, -0, 7, NaN, Infinity, -Infinity, undefined as unknown as number];
const bits = new BigUint64Array([0x7ff8000000000001n]);
values.push(new Float64Array(bits.buffer)[0]);
for (const value of values) {
    const object = make(value), result = read(object);
    console.log("read", String(result), typeof result, Object.is(result, -0), Number.isNaN(result), Object.hasOwn(object, "value"));
}
const object = make(7);
const alias: any = object;
for (const value of values) {
    alias.value = value;
    const result = read(object);
    console.log("write", String(result), typeof result, Object.is(result, -0), Number.isNaN(result));
}
const mixed = { number: 7, text: "seven", flag: true };
function property(target: any, key: string): any { return target[key]; }
console.log("other fields", property(mixed, "number"), property(mixed, "text"), property(mixed, "flag"), property(mixed, "missing"));
