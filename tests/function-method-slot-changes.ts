export {};
function fn(value: number): number { return value + 1; }
const prototype = Object.getPrototypeOf(fn);
const call = Object.getOwnPropertyDescriptor(prototype, "call")!;
const apply = Object.getOwnPropertyDescriptor(prototype, "apply")!;
const results: any[] = [];
results.push(fn.call(null, 1), fn.apply(null, [2]));
try {
    let evaluated = 0;
    for (const value of [17, 19]) {
        prototype.apply = value;
        try { fn.apply(null, [++evaluated]); } catch (error) {
            results.push("not callable", evaluated, (error as Error).name);
        }
    }
    Object.defineProperty(prototype, "apply", apply);
    results.push(fn.apply(null, [2]));
    // Removing the first property moves later inline slots.
    delete prototype.call;
    results.push(fn.apply(null, [3]));
    Object.defineProperty(prototype, "call", call);
    results.push(fn.call(null, 4));
    delete prototype.apply;
    results.push(fn.call(null, 5));
    Object.defineProperty(prototype, "apply", apply);
    results.push(fn.apply(null, [6]));
    // Reinserted properties can now be stored outside the inline slots.
    prototype.apply = function(this: any, receiver: unknown, values: number[]): number {
        return values[0] + 20;
    };
    results.push(fn.apply(null, [7]));
    Object.defineProperty(prototype, "apply", apply);
    Object.defineProperty(prototype, "unrelated", { configurable: true, value: 17 });
    results.push(fn.call(null, 8), fn.apply(null, [9]));
    delete prototype.unrelated;
} finally {
    Object.defineProperty(prototype, "call", call);
    Object.defineProperty(prototype, "apply", apply);
}
console.log(results.join(","));
