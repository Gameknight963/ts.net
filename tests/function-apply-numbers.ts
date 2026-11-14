export {};
function read(this: any, value: number): number { return this.base + value; }
console.log("apply", read.apply({ base: 7 }, [3]));
function count(this: any, value: number): number { return value + arguments.length + this.base; }
console.log("arguments", count.apply({ base: 7 }, [3]));
function defaults(this: any, value = 5): number { return this.base + value; }
console.log("defaults", defaults.apply({ base: 7 }, []), defaults.apply({ base: 7 }, [undefined]));
function rest(this: any, ...values: number[]): number { return this.base + values.length + values[0]; }
console.log("rest", rest.apply({ base: 7 }, [3]));
function optional(this: any, value: number): number | undefined { return value === 0 ? undefined : value + this.base; }
console.log("optional", optional.apply({ base: 7 }, [0]), optional.apply({ base: 7 }, [3]));
console.log("bound", read.bind({ base: 11 }).apply({ base: 99 }, [3]));
console.log("arrow", ((value: number) => value + 1).apply({ base: 99 }, [3]));
const log: string[] = [];
const original = read.apply;
Object.defineProperty(read, "apply", { configurable: true, get() { log.push("callee"); return original; } });
function receiver(): any {
    log.push("receiver");
    Object.defineProperty(read, "apply", { configurable: true, value: function(this: any, target: any, values: number[]): number {
        log.push("custom");
        console.log("custom array", this === read, arguments.length, Array.isArray(values), values.length, Object.getPrototypeOf(values) === Array.prototype, 0 in values);
        return target.base * values[0];
    } });
    return { base: 7 };
}
function argument(): number { log.push("element"); return 3; }
console.log("saved", read.apply(receiver(), [argument()]), log.join(","));
log.length = 0;
console.log("replaced", read.apply({ base: 7 }, [argument()]), log.join(","));
function missing(): number { return undefined as any; }
console.log("missing literal", read.apply({ base: 7 }, [missing()]));
delete (read as any).apply;
console.log("restored", read.apply({ base: 7 }, [3]));
const tuple: [number] = [3];
console.log("variable", read.apply({ base: 7 }, tuple), "spread", read.apply({ base: 7 }, [...tuple]));
console.log("negative zero", Object.is(((value: number) => value).apply(null, [-0]), -0));
function throws(this: any, value: number): number { throw this.base + value; }
try { throws.apply({ base: 7 }, [3]); } catch (error) { console.log("throw", error); }
