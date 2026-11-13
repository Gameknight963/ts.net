class LoopFirst { base = 2; read(value: number): number { return this.base + value; } }
class LoopSecond { base = 3; read(value: number): number { return this.base * 2 + value; } }
const loopFirst = new LoopFirst(), loopSecond = new LoopSecond();
function forwardLoop(target: {read(value: number): number}, value: number): number { return target.read(value); }
function mixedLoop(count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += forwardLoop(index & 1 ? loopFirst : loopSecond, index);
    return total;
}
function directMixedLoop(count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += (index & 1 ? loopFirst : loopSecond).read(index);
    return total;
}
console.log('mixed', mixedLoop(64), directMixedLoop(64), mixedLoop(1), mixedLoop(0), mixedLoop(NaN));
loopFirst.base = 8;
console.log('updated', mixedLoop(64), directMixedLoop(64));
let reads = 0;
Object.defineProperty(loopSecond, 'base', {configurable: true, get() { return ++reads; }});
console.log('fields', mixedLoop(64), reads);
Object.defineProperty(loopSecond, 'base', {value: 3, configurable: true, writable: true});
const descriptor = Object.getOwnPropertyDescriptor(LoopSecond.prototype, 'read')!;
const original = LoopSecond.prototype.read;
let calls = 0;
try {
    Object.defineProperty(LoopSecond.prototype, 'read', {configurable: true, get() { calls++; loopFirst.base++; return original; }});
    console.log('methods', mixedLoop(64), calls, loopFirst.base);
} finally { Object.defineProperty(LoopSecond.prototype, 'read', descriptor); }
Object.defineProperty(loopSecond, 'read', {value: (value: number) => value + 30, configurable: true});
console.log('own', mixedLoop(64));
delete (loopSecond as any).read;
console.log('restored', mixedLoop(64));
function unusedPending(): number {
    let total = 0;
    for (let index = 0; index < 64; index++) total += forwardLoop(index & 0 ? pending : loopFirst, index);
    return total;
}
function usedPending(count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += forwardLoop(index & 1 ? pending : loopFirst, index);
    return total;
}
console.log('unused-tdz', unusedPending(), usedPending(0), usedPending(1));
try { usedPending(64); } catch (error) { console.log('used-tdz', (error as Error).name); }
const pending = new LoopSecond();
console.log('ready', unusedPending(), usedPending(64));
const absent = undefined as unknown as LoopSecond;
function unusedAbsent(): number {
    let total = 0;
    for (let index = 0; index < 64; index++) total += forwardLoop(index & 0 ? absent : loopFirst, index);
    return total;
}
console.log('unused-undefined', unusedAbsent());

function maskedLoop(): number {
    let total = 0;
    for (let index = 3; index < 67; index++) total += forwardLoop(index & 2 ? loopFirst : pending, index);
    return total;
}
function fractionalMask(): number {
    let total = 0;
    for (let index = 0; index < 64; index++) total += forwardLoop(index & 1.5 ? loopFirst : pending, index);
    return total;
}
console.log('mask', maskedLoop(), fractionalMask());
Object.freeze(loopFirst); Object.freeze(pending);
console.log('frozen', usedPending(64), maskedLoop());
