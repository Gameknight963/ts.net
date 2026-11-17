class Point {
 value = "initial";
 count = 0;
 constructor(value: string) { this.value = value; }
 write(value: string) { this.value = value; return this.value; }
 step() { return ++this.count; }
}
const point = new Point("first");
console.log(point.write("second"), point.step(), point.count);
const borrowed = { value: "old" };
console.log(point.write.call(borrowed as Point, "borrowed"), borrowed.value);
const changes: string[] = [];
const proxy = new Proxy(point, { set(target, key, value, receiver) { changes.push(String(key)); return Reflect.set(target, key, value, receiver); } });
console.log(proxy.write("through proxy"), changes.join(","));
Object.defineProperty(point, "value", { get() { return "getter"; }, set(value: string) { changes.push(value); }, configurable: true });
console.log(point.write("setter"), changes.join(","));
const missing = new Point("deleted");
delete (missing as {value?: string}).value;
const prototype = Object.getPrototypeOf(missing);
Object.defineProperty(prototype, "value", { set(value: string) { changes.push(value); }, configurable: true });
console.log(missing.write("inherited"), changes.join(","));
delete prototype.value;
const frozen = Object.freeze(new Point("frozen"));
try { frozen.write("fail"); } catch { console.log("frozen rejected"); }
class Derived extends Point { extra = true; }
const derived = new Derived("derived");
console.log(derived.write("derived update"), derived.step(), derived.extra);
const numeric = new Point("number");
numeric.count += 2; console.log(numeric.count);
Object.defineProperty(numeric, "count", { get() { return 3; }, set(value: number) { changes.push(String(value)); } });
console.log(numeric.step(), changes[changes.length - 1]);

class Looped {
    value = "";
    unused = 0;
    constructor(count: number) {
        for (let i = 0; i < count; i++) this.value = (i & 1) === 0 ? "left" : "right";
    }
}
export function work(count: number): number { return new Looped(count).value.length; }
console.log(work(10));
