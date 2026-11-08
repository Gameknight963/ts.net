/// <reference lib="es2024.arraybuffer" />
export {};
for (const length of [0, 3]) {
    const array = new Uint8Array(length);
    let count = 0, order = '';
    const value = {valueOf() {order += 'value|'; return ++count;}};
    const start = {valueOf() {order += 'start|'; return 0;}};
    const end = {valueOf() {order += 'end|'; return length;}};
    console.log('number', array.fill(value as any, start as any, end as any) === array, array.join(','), count, order);
}
let bigCount = 0;
const big = new BigInt64Array(3);
big.fill({valueOf() {bigCount++; return 7n;}} as any);
console.log('bigint', big.join(','), bigCount);
for (const array of [new Uint8Array(0), new BigInt64Array(0)]) {
    let starts = 0;
    try {(array as any).fill(Symbol('invalid') as any, {valueOf() {starts++; return 0;}} as any); console.log('unexpected');}
    catch (error) {console.log('invalid', (error as Error).name, starts);}
}
for (const empty of [false, true]) {
    const buffer = new ArrayBuffer(3), array = new Uint8Array(buffer);
    const value = {valueOf() {buffer.transfer(); return 1;}};
    try {array.fill(value as any, 0, empty ? 0 : 3); console.log('detached accepted');}
    catch (error) {console.log('detached', empty, (error as Error).name);}
}
for (const tracking of [false, true]) {
    const buffer = new ArrayBuffer(3, {maxByteLength: 6});
    const array = tracking ? new Uint8Array(buffer) : new Uint8Array(buffer, 0, 3);
    let order = '';
    const start = {valueOf() {order += 'start|'; buffer.resize(1); return 0;}};
    const end = {valueOf() {order += 'end|'; return 3;}};
    try {array.fill(7, start as any, end as any); console.log('shrink', tracking, order, array.join(','));}
    catch (error) {console.log('shrink error', tracking, order, (error as Error).name);}
}
const growing = new ArrayBuffer(3, {maxByteLength: 6});
const tracked = new Uint8Array(growing);
tracked.fill({valueOf() {growing.resize(6); return 8;}} as any);
console.log('grow', tracked.join(','));
