/// <reference lib="es2023.array" />
/// <reference lib="es2024.arraybuffer" />
export {};
const array = new Uint8Array([1, 2, 3]);
let order = '';
const index = {valueOf() {order += 'index|'; array[1] = 7; return 0;}};
const value = {valueOf() {order += 'value|'; array[2] = 8; return 9;}};
console.log('mutation', array.with(index as any, value as any).join(','), array.join(','), order);
for (const constructor of [Uint8Array, BigInt64Array]) {
    try {new constructor(1).with(99, Symbol('value') as never); console.log('unexpected');}
    catch (error) {console.log('conversion first', constructor.name, (error as Error).name);}
}
for (const tracking of [false, true]) {
    const buffer = new ArrayBuffer(3, {maxByteLength: 6});
    const input = tracking ? new Uint8Array(buffer) : new Uint8Array(buffer, 0, 3);
    input.set([1, 2, 3]);
    const replacement = {valueOf() {buffer.resize(1); return 9;}};
    try {console.log('shrink', tracking, input.with(0, replacement as any).join(','));}
    catch (error) {console.log('shrink error', tracking, (error as Error).name);}
}
const buffer = new ArrayBuffer(3, {maxByteLength: 6}), input = new Uint8Array(buffer);
input.set([1, 2, 3]);
try {console.log('grow index', input.with(3, {valueOf() {buffer.resize(6); return 9;}} as any).join(','));}
catch (error) {console.log('grow error', (error as Error).name);}
const detached = new Uint8Array([1, 2]);
try {detached.with(0, {valueOf() {detached.buffer.transfer(); return 9;}} as any); console.log('detached accepted');}
catch (error) {console.log('detached', (error as Error).name);}
let bigConversions = 0;
const big = new BigInt64Array([1n, 2n]);
console.log('bigint', big.with(-1, {valueOf() {bigConversions++; return 9n;}} as any).join(','), big.join(','), bigConversions);
const empty = new Uint8Array(0);
let converted = 0;
try {empty.with(0, {valueOf() {converted++; return 1;}} as any);} catch (error) {console.log('empty', (error as Error).name, converted);}
const original = new Float64Array([1, 2]);
Object.defineProperty(original, 'length', {get() {throw new Error('public length');}});
console.log('internal length', original.with(-1, 3).join(','));
