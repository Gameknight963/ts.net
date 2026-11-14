export {};
function read(this: any, value: number): number { return this.base + value; }
console.log("call", read.call({ base: 7 }, 3));
function count(this: any, value: number): number { return value + arguments.length + this.base; }
console.log("arguments", count.call({ base: 7 }, 3));
function defaults(this: any, value = 5): number { return this.base + value; }
console.log("defaults", defaults.call({ base: 7 }, undefined));
function rest(this: any, ...values: number[]): number { return this.base + values.length + values[0]; }
console.log("rest", rest.call({ base: 7 }, 3));
function optional(this: any, value: number): number | undefined { return value === 0 ? undefined : value + this.base; }
console.log("optional", optional.call({ base: 7 }, 0), optional.call({ base: 7 }, 3));
console.log("bound", read.bind({ base: 11 }).call({ base: 99 }, 3));
console.log("arrow", ((value: number) => value + 1).call({ base: 99 }, 3));

const log: string[] = [];
const original = read.call;
Object.defineProperty(read, "call", { configurable: true, get() { log.push("callee"); return original; } });
function receiver(): any {
    log.push("receiver");
    Object.defineProperty(read, "call", { configurable: true, value: function(this: any, target: any, value: number): number {
        log.push("custom");
        console.log("custom receiver", this === read, arguments.length);
        return target.base * value;
    } });
    return { base: 7 };
}
function argument(): number { log.push("argument"); return 3; }
console.log("saved", read.call(receiver(), argument()), log.join(","));
log.length = 0;
console.log("replaced", read.call({ base: 7 }, argument()), log.join(","));
delete (read as any).call;
console.log("restored", read.call({ base: 7 }, 3));
const values: [any, number] = [{ base: 7 }, 3];
console.log("spread", read.call(...values));
console.log("extra", (read.call as any)({ base: 7 }, 3, 9));
function throws(this: any, value: number): number { throw this.base + value; }
try { throws.call({ base: 7 }, 3); } catch (error) { console.log("throw", error); }
