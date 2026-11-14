export {};
function fn(value: number): number { return value + 1; }
const prototype = Object.getPrototypeOf(fn);
const descriptor = Object.getOwnPropertyDescriptor(prototype, "call")!;
const original = descriptor.value;
const results: any[] = [];
results.push(fn.call(null, 3));
try {
    prototype.call = function(this: any, receiver: any, value: number): number { return value + 10; };
    results.push(fn.call(null, 3));
    prototype.call = function(this: any, receiver: any, value: number): number { return value + 20; };
    results.push(fn.call(null, 3));
    Object.defineProperty(prototype, "call", { ...descriptor, value: function(this: any, receiver: any, value: number): number { return value + 30; } });
    results.push(fn.call(null, 3));
    prototype.call = null;
    let evaluated = 0;
    try { fn.call(null, ++evaluated); } catch { results.push("not callable", evaluated); }
    Object.defineProperty(prototype, "call", descriptor);
    let inheritedReads = 0, inheritedReceiver = false;
    Object.defineProperty(Object.prototype, "call", { configurable: true, get() {
        inheritedReads++;
        inheritedReceiver = this === fn;
        return original;
    } });
    try {
        results.push(fn.call(null, 3), inheritedReads);
        delete prototype.call;
        results.push(fn.call(null, 3), inheritedReads, inheritedReceiver);
    } finally {
        Object.defineProperty(prototype, "call", descriptor);
        delete (Object.prototype as any).call;
    }
    let replacedReads = 0;
    Object.defineProperty(prototype, "call", { configurable: true, get() {
        replacedReads++;
        Object.defineProperty(prototype, "call", descriptor);
        return original;
    } });
    results.push(fn.call(null, 3), fn.call(null, 3), replacedReads);
} finally { Object.defineProperty(prototype, "call", descriptor); }
let ownReads = 0, ownReceiver = false;
Object.defineProperty(fn, "call", { configurable: true, get() { ownReads++; ownReceiver = this === fn; return original; } });
results.push(fn.call(null, 3), ownReads, ownReceiver);
delete (fn as any).call;
const ordinary: any = { base: 7, call(value: number): number { return this.base + value; } };
results.push(ordinary.call(3));
console.log(results.join(","));

const stored = (value: number): number => value + 1;
Object.defineProperty(stored, "call", { configurable: true, writable: true, value: function(receiver: unknown, value: number): number { return value + 10; } });
console.log("stored", stored.call(null, 3));
(stored as any).call = function(receiver: unknown, value: number): number { return value + 20; };
Object.setPrototypeOf(stored, { get call() { throw new Error("own method should win"); } });
console.log("overwritten", stored.call(null, 3));
Object.defineProperty(stored, "call", { configurable: true, get() { return original; } });
console.log("accessor replacement", stored.call(null, 3));
Object.defineProperty(stored, "call", { configurable: true, get: undefined });
let missingArguments = 0;
try { stored.call(null, ++missingArguments); } catch { console.log("missing getter", missingArguments); }
const tagged = (value: number): number => value + 1;
(tagged as any).metadata = 7;
console.log("unrelated property", tagged.call(null, 3));
