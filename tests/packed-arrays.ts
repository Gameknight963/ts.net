function packed(values: number[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        values[i] += 1;
        total += values[i];
    }
    return total;
}
const values = [1, 2, 3];
console.log(packed(values, 3), values[0], values[2]);
console.log(packed(values, 2.5), packed(values, 0), packed(values, -1));
const holes = [1, , 3] as number[];
console.log(packed(holes, 3), holes[1], holes[1] === undefined);
console.log(packed(holes, 3));
const generic: any[] = [1, 2, 3];
console.log(packed(generic, 3), generic[0]);
const short = [1];
console.log(packed(short, 3), short.length, short[2]);
values.length = 0;
values.length = 3;
console.log(packed(values, 3));
values.length = 0;
values.push(4, 5, 6);
console.log(packed(values, 3));

function clear(values: number[], count: number, value: any) {
    for (let i = 0; i < count; i++) values[i] = value;
}
clear(values, 3, undefined);
console.log(values[0], packed(values, 3));
function nan(values: number[], count: number) {
    for (let i = 0; i < count; i++) values[i] = 0 / 0;
}
const nans = [1, 2, 3];
nan(nans, 3);
console.log(nans[0], nans[0] === undefined, packed(nans, 3));
function aliased(values: number[], other: number[], count: number) {
    for (let i = 0; i < count; i++) {
        other.length = 0;
        values[i] += 1;
    }
}
const alias = [1, 2, 3];
aliased(alias, alias, 3);
console.log(alias.length, alias[0], alias[2]);
function shrink(values: number[]) { values.length = 0; return 3; }
function calls(values: number[], count: number) {
    for (let i = 0; i < count; i++) values[i] += shrink(values);
}
const called = [7, 8, 9];
calls(called, 3);
console.log(called.length, called[0], called[2]);
function rebind(values: number[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) {
        if (i === 1) values = [9, 10, 11];
        total += values[i];
    }
    return total;
}
console.log(rebind([1, 2, 3], 3));
function fixed(values: number[], count: number) {
    let total = 0;
    for (let i = 0; i < count; i++) total += values[0];
    return total;
}
console.log(fixed([3], 7));
function pushed(count: number) {
    const values: number[] = [];
    for (let i = 0; i < count; i++) values.push(values.length + i);
    return values[0] + values[1] + values[2] + values.length;
}
console.log(pushed(3), pushed(2.5));
function payload(values: number[], count: number, seed: number) {
    for (let i = 0; i < count; i++) values[i] = seed + 0;
}
const payloadValues = [1, 2, 3];
payload(payloadValues, 3, 0 / 0);
console.log(payloadValues[0], payloadValues[0] === undefined);
function zero(values: number[], count: number) {
    for (let i = 0; i < count; i++) values[i] = -0;
}
zero(payloadValues, 3);
console.log(payloadValues[0], 1 / payloadValues[0]);
function redeclaredBound(count: number) {
    let visits = 0;
    for (let i = 0; i < count; i++) { var count = 0; visits++; }
    return visits;
}
console.log(redeclaredBound(4));
function redeclaredArray() {
    var values = [1, 2, 3];
    let total = 0;
    for (let i = 0; i < 3; i++) {
        var values = [9, 10, 11];
        total += values[i];
    }
    return total;
}
console.log(redeclaredArray());
function arrayDeclaredInLoop() {
    let total = 0;
    for (let i = 0; i < 3; i++) {
        const local = [4, 5, 6];
        total += local[i];
    }
    return total;
}
console.log(arrayDeclaredInLoop());
function redeclaredValue() {
    var value = 0 / 0;
    console.log(value);
    var value = 3;
    console.log(value);
    var value: number;
    console.log(value);
}
redeclaredValue();
