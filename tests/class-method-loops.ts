export {};
class MethodCounter { base = 1; read(value: number): number { return this.base + value; } }
function forward(target: {read(value: number): number}, value: number): number { return target.read(value); }
function accumulate(target: MethodCounter, count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += forward(target, index);
    return total;
}
const target = new MethodCounter();
console.log('plain', accumulate(target, 64));
target.base = 4;
console.log('changed', accumulate(target, 64));
let fields = 0, methods = 0;
Object.defineProperty(target, 'base', {configurable: true, get() { fields++; return fields; }});
console.log('field-accessor', accumulate(target, 64), fields);
Object.defineProperty(target, 'base', {configurable: true, value: 3, writable: true});
const original = MethodCounter.prototype.read;
Object.defineProperty(target, 'read', {configurable: true, value: (value: number) => value + 10});
console.log('own-method', accumulate(target, 64));
delete (target as any).read;
const descriptor = Object.getOwnPropertyDescriptor(MethodCounter.prototype, 'read')!;
try {
    Object.defineProperty(MethodCounter.prototype, 'read', {configurable: true, get() { methods++; target.base++; return original; }});
    console.log('method-accessor', accumulate(target, 64), methods, target.base);
} finally { Object.defineProperty(MethodCounter.prototype, 'read', descriptor); }
Object.defineProperty(target, 'read', {configurable: true, value: original.bind({base: 9})});
console.log('bound', accumulate(target, 64));
delete (target as any).read;
Object.setPrototypeOf(target, {read(value: number) { return value + 20; }});
console.log('prototype', accumulate(target, 64));
Object.setPrototypeOf(target, MethodCounter.prototype);
console.log('frozen', accumulate(Object.freeze(target), 64));
console.log('empty', accumulate(undefined as unknown as MethodCounter, 0), accumulate(undefined as unknown as MethodCounter, NaN));
class Derived extends MethodCounter { read(value: number): number { methods++; return value + 30; } }
console.log('derived', accumulate(new Derived(), 64), methods);
const structural = {base: 5, read(value: number) { methods++; return this.base + value; }};
console.log('structural', accumulate(structural, 64), methods);
function delayedSum(count: number): number {
    let total = 0;
    for (let index = 0; index < count; index++) total += forward(delayed, index);
    return total;
}
console.log('tdz-empty', delayedSum(0));
try { delayedSum(64); } catch (error) { console.log('tdz-positive', (error as Error).name); }
const delayed = new MethodCounter();
console.log('tdz-ready', delayedSum(64));
class Missing { base!: number; read(value: number): number { return this.base + value; } }
function missing(target: Missing): number {
    let total = 0;
    for (let index = 0; index < 64; index++) total += forward(target, index);
    return total;
}
console.log('missing', Number.isNaN(missing(new Missing())));
class Echo { read(value: number): number { return value; } }
function echo(target: Echo): number {
    let total = 0;
    for (let index = 0; index < 64; index++) total += forward(target, index);
    return total;
}
console.log('echo', echo(new Echo()));
function writes(target: MethodCounter): number {
    let total = 0;
    for (let index = 0; index < 64; index++) { target.base = index; total += forward(target, index); }
    return total;
}
console.log('writes', writes(new MethodCounter()));
let helperCalls = 0;
function observedForward(target: {read(value: number): number}, value: number): number { helperCalls++; return target.read(value); }
function observedHelper(target: MethodCounter): number {
    let total = 0;
    for (let index = 0; index < 64; index++) total += observedForward(target, index);
    return total;
}
console.log('helper-effects', observedHelper(new MethodCounter()), helperCalls);
class NegativeZero { read(value: number): number { return -0; } }
function negativeZero(target: NegativeZero): number {
    let total = -0;
    for (let index = 0; index < 64; index++) total += forward(target, index);
    return total;
}
console.log('zero', Object.is(negativeZero(new NegativeZero()), -0));
