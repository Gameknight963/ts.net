export {};
type Item = { count: number };
type Callback = (item: Item, index: number) => number;
function invoke(callback: Callback, item: Item, index: number): number { return callback(item, index); }
function add(item: Item, index: number): number { return item.count + index; }
console.log("pair", invoke(add, { count: 7 }, 3), add.length, Object.getPrototypeOf(add) === Function.prototype);
function factory(offset: number): Callback { return (item, index) => offset + item.count + index; }
console.log("closure", invoke(factory(11), { count: 7 }, 3));
function objects(callback: (a: Item, b: Item) => number, a: Item, b: Item): number { return callback(a, b); }
console.log("objects", objects((a, b) => a.count + b.count, { count: 7 }, { count: 13 }));
function strings(callback: (a: string, b: string) => number): number { return callback("ab", "cde"); }
console.log("strings", strings((a, b) => a.length + b.length));
function defaultStrings(callback: (a: string, b?: string) => number): number { return callback("abc", undefined); }
console.log("string default", defaultStrings((a, b = "xy") => a.length + b.length));
function missing(): number { return undefined as any; }
console.log("missing", invoke((item, index) => index, { count: 7 }, missing()));
console.log("negative zero", Object.is(invoke((item, index) => index, { count: 7 }, -0), -0));
console.log("default", invoke((item, index = 19) => item.count + index, { count: 7 }, missing()));
let defaults = 0;
function dependentDefault(item: Item, index = item.count + ++defaults): number { return item.count + index; }
console.log("dependent default", invoke(dependentDefault, { count: 7 }, missing()), invoke(dependentDefault, { count: 7 }, 3), defaults);
function capturedDefault(offset: number): Callback { return (item, index = ++offset) => item.count + index; }
const captured = capturedDefault(11);
console.log("captured default", invoke(captured, { count: 7 }, missing()), invoke(captured, { count: 7 }, 3), invoke(captured, { count: 7 }, missing()));
function firstDefault(index = 19, item: Item): number { return item.count + index; }
function invokeFirst(callback: (index: number, item: Item) => number, index: number, item: Item): number { return callback(index, item); }
console.log("first default", invokeFirst(firstDefault, missing(), { count: 7 }), invokeFirst(firstDefault, 3, { count: 7 }));
function invokeOptional(callback: (item: Item, index?: number) => number, item: Item, index?: number): number { return callback(item, index); }
const optional = (item: Item, index?: number): number => item.count + (index ?? 29);
console.log("optional", invokeOptional(optional, { count: 7 }), invokeOptional(optional, { count: 7 }, 3));
console.log("rest", invoke((item, ...values: number[]) => item.count + values[0] + values.length, { count: 7 }, 3));
console.log("arguments", invoke(function(item: Item, index: number): number { return item.count + index + arguments.length; }, { count: 7 }, 3));
const method = { base: 11, read(item: Item, index: number): number { return this.base + item.count + index; } };
function invokeMethod(target: { read(item: Item, index: number): number }): number { return target.read({ count: 7 }, 3); }
console.log("receiver", invokeMethod(method));
const defaultMethod = { base: 23, read(this: { base: number }, item: Item, index = this.base): number { return item.count + index; } };
function invokeDefaultMethod(target: { read(item: Item, index?: number): number }): number { return target.read({ count: 7 }, missing()); }
console.log("receiver default", invokeDefaultMethod(defaultMethod));
const plain = { read: add };
console.log("unused receiver", invokeMethod(plain));
console.log("bound", invoke(method.read.bind(method), { count: 7 }, 3));
const log: string[] = [];
const holder: { read: Callback } = { read: add };
Object.defineProperty(holder, "read", { configurable: true, get() { log.push("callee"); return add; } });
function first(): Item {
    log.push("first");
    Object.defineProperty(holder, "read", { configurable: true, value: (item: Item, index: number): number => item.count * index });
    return { count: 7 };
}
function second(): number { log.push("second"); return 3; }
console.log("saved", holder.read(first(), second()), log.join(","));
console.log("replaced", holder.read({ count: 7 }, 3));
let evaluated = 0;
const invalid = 42 as unknown as Callback;
try { invoke(invalid, { count: ++evaluated }, ++evaluated); } catch { console.log("invalid", evaluated); }
try { invoke(() => { throw new Error("callback failure"); }, { count: 7 }, 3); }
catch (error) { console.log((error as Error).message); }
