export {};
function target(value: number): number { return value; }
function install(callback: Function): void {
    Object.defineProperty(target, "apply", { configurable: true, value: callback });
}
install((receiver: { base: number }, values: number[]): number => receiver.base + values[0]);
console.log("pair", target.apply({ base: 7 }, [3]));
function factory(offset: number): Function {
    return (receiver: { base: number }, values: number[]): number => offset + receiver.base + values[0];
}
install(factory(11));
console.log("closure", target.apply({ base: 7 }, [3]));
install((receiver: any, values: number[]): number => {
    console.log("array", Array.isArray(values), values.length, 0 in values, Object.getPrototypeOf(values) === Array.prototype);
    values[0] += receiver.base;
    return values[0];
});
console.log("mutation", target.apply({ base: 7 }, [3]));
install((receiver: any, values: number[]): number => values[0]);
function missing(): number { return undefined as any; }
console.log("missing", target.apply(null, [missing()]));
console.log("negative zero", Object.is(target.apply(null, [-0]), -0));
install(function(this: any, receiver: any, values: number[]): number {
    console.log("receiver", this === target, arguments.length);
    return receiver.base + values[0];
});
console.log("ordinary fallback", target.apply({ base: 7 }, [3]));
install((receiver: any, values: number[] = [99]): number => receiver.base + values[0]);
console.log("default fallback", target.apply({ base: 7 }, [3]));
install((receiver: any, ...values: number[][]): number => receiver.base + values[0][0]);
console.log("rest fallback", target.apply({ base: 7 }, [3]));
install((receiver: any, values: number[]): number => { throw new Error("pair failure"); });
try { target.apply(null, [3]); } catch (error) { console.log((error as Error).message); }
delete (target as any).apply;
console.log("intrinsic", target.apply(null, [3]));
