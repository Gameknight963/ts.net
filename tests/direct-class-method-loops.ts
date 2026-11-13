class DirectCounter { base = 2; read(value: number): number { return this.base * 2 + value; } }
function directSum(target: DirectCounter, count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += target.read(index);
    return total;
}
const target = new DirectCounter();
console.log('plain', directSum(target, 64));
target.base = 5;
console.log('updated', directSum(target, 64));
let fields = 0, calls = 0;
Object.defineProperty(target, 'base', {configurable: true, get() { fields++; return fields; }});
console.log('fields', directSum(target, 64), fields);
Object.defineProperty(target, 'base', {value: 3, configurable: true, writable: true});
const descriptor = Object.getOwnPropertyDescriptor(DirectCounter.prototype, 'read')!;
const original = DirectCounter.prototype.read;
try {
    Object.defineProperty(DirectCounter.prototype, 'read', {configurable: true, get() { calls++; target.base++; return original; }});
    console.log('methods', directSum(target, 64), calls, target.base);
} finally { Object.defineProperty(DirectCounter.prototype, 'read', descriptor); }
Object.defineProperty(target, 'read', {configurable: true, value: (value: number) => value + 8});
console.log('own', directSum(target, 64));
delete (target as any).read;
console.log('restored', directSum(target, 64));
console.log('frozen', directSum(Object.freeze(target), 64));
console.log('empty', directSum(undefined as unknown as DirectCounter, 0), directSum(undefined as unknown as DirectCounter, NaN));
class Derived extends DirectCounter { read(value: number): number { calls++; return value; } }
console.log('derived', directSum(new Derived(), 64), calls);
console.log('structural', directSum({base: 0, read(value: number) { calls++; return value + 3; }}, 64), calls);
function uninitialized(count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += later.read(index);
    return total;
}
console.log('tdz-empty', uninitialized(0));
try { uninitialized(64); } catch (error) { console.log('tdz', (error as Error).name); }
const later = new DirectCounter();
console.log('tdz-ready', uninitialized(64));
