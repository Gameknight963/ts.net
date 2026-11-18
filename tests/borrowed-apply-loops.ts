export {};
const counter = {base: 1, read(value: number): number { return this.base + value; }};
let borrowed = counter.read;
let other = {base: 2};
function invoke(target: {read(value: number): number}, value: number): number { return borrowed.apply(other as any, [value]); }
function sum(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) total += invoke(counter, i);
    return total;
}
function direct(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) total += borrowed.apply(other as any, [i]);
    return total;
}
console.log(sum(64), direct(64), sum(0), sum(31), sum(32));
let argumentReads = 0;
const proxyArgument = new Proxy(counter, {
    get(target, key, receiver) { argumentReads++; return Reflect.get(target, key, receiver); },
    has(target, key) { argumentReads++; return Reflect.has(target, key); }
});
function proxyInput(): number {
    let total = 0;
    for (let i = 0; i < 64; i++) total += invoke(proxyArgument, i);
    return total;
}
console.log(proxyInput(), argumentReads);
other.base = 5;
console.log(sum(64), direct(64));
let calls = 0, fresh = 0;
let previous: number[] | undefined;
Object.defineProperty(borrowed, 'apply', {configurable: true, value: function(target: {base: number}, values: number[]): number {
    calls++; if (values !== previous) fresh++; previous = values; values[0] += 10; return target.base + values[0];
}});
console.log(sum(64), direct(64), calls, fresh, previous!.length);
delete (borrowed as any).apply;
const builtinApply = Function.prototype.apply;
let gets = 0;
Object.defineProperty(borrowed, 'apply', {configurable: true, get() { gets++; return builtinApply; }});
console.log(sum(64), direct(64), gets);
function pending(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) total += invoke(later, i);
    return total;
}
console.log(pending(0));
try { pending(64); } catch (error) { console.log((error as Error).name, gets); }
const later = counter;
console.log(pending(64), gets);
delete (borrowed as any).apply;
const original = borrowed;
borrowed = function(value: number): number { return value * 2; };
console.log(sum(64), direct(64));
borrowed = original;
const saved = other;
let reads = 0;
other = new Proxy(saved, {get(target, key, receiver) { if (key === 'base') reads++; return Reflect.get(target, key, receiver); }});
console.log(sum(64), direct(64), reads);
other = null as any;
console.log(sum(0));
try { sum(64); } catch (error) { console.log((error as Error).name); }
other = saved;
console.log(sum(64));
const observedArguments: [number] = [0];
let valueReads = 0;
Object.defineProperty(observedArguments, '0', {get() { valueReads++; return valueReads; }});
function arrayInput(): number {
    let total = 0;
    for (let i = 0; i < 64; i++) total += borrowed.apply(other as any, observedArguments);
    return total;
}
console.log(arrayInput(), valueReads);
