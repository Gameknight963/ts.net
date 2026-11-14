export {};
function fn(value: number): number { return value + 1; }
const prototype = Object.getPrototypeOf(fn);
const descriptor = Object.getOwnPropertyDescriptor(prototype, "apply")!;
const original = descriptor.value;
const results: any[] = [];
results.push(fn.apply(null, [3]));
try {
    prototype.apply = function(this: any, receiver: any, values: number[]): number { return values[0] + 10; };
    results.push(fn.apply(null, [3]));
    prototype.apply = function(this: any, receiver: any, values: number[]): number { return values[0] + 20; };
    results.push(fn.apply(null, [3]));
    Object.defineProperty(prototype, "apply", { ...descriptor, value: function(this: any, receiver: any, values: number[]): number { return values[0] + 30; } });
    results.push(fn.apply(null, [3]));
    prototype.apply = null;
    let evaluated = 0;
    try { fn.apply(null, [++evaluated]); } catch { results.push("not callable", evaluated); }
    Object.defineProperty(prototype, "apply", descriptor);
    let inheritedReads = 0, inheritedReceiver = false;
    Object.defineProperty(Object.prototype, "apply", { configurable: true, get() {
        inheritedReads++;
        inheritedReceiver = this === fn;
        return original;
    } });
    try {
        results.push(fn.apply(null, [3]), inheritedReads);
        delete prototype.apply;
        results.push(fn.apply(null, [3]), inheritedReads, inheritedReceiver);
    } finally {
        Object.defineProperty(prototype, "apply", descriptor);
        delete (Object.prototype as any).apply;
    }
    let replacedReads = 0;
    Object.defineProperty(prototype, "apply", { configurable: true, get() {
        replacedReads++;
        Object.defineProperty(prototype, "apply", descriptor);
        return original;
    } });
    results.push(fn.apply(null, [3]), fn.apply(null, [3]), replacedReads);
} finally { Object.defineProperty(prototype, "apply", descriptor); }
let ownReads = 0, ownReceiver = false;
Object.defineProperty(fn, "apply", { configurable: true, get() { ownReads++; ownReceiver = this === fn; return original; } });
results.push(fn.apply(null, [3]), ownReads, ownReceiver);
delete (fn as any).apply;
const ordinary: any = { base: 7, apply(value: number): number { return this.base + value; } };
results.push(ordinary.apply(3));
console.log(results.join(","));
