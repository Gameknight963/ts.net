export {};
class Base {
    value = 0;
    static received = 0;
    get item(): number { throw new Error("assignment called getter"); }
    set item(value: number) { this.value = value; console.log("setter", value, this instanceof Derived); }
    set key(value: string) { console.log("key", value, this instanceof Derived); }
    set rejected(value: number) { throw new Error("rejected " + value); }
    static set item(value: number) { this.received = value; }
}
class Derived extends Base {
    name(): "item" { console.log("computed key"); return "item"; }
    run(): void {
        for (super.item of [1, 2]) console.log("value", this.value);
        for (super[this.name()] of [3, 4]) console.log("computed", this.value);
        for (super.key in {a: 1, b: 2}) {}
    }
    reject(source: Iterable<number>): void {
        for (super.rejected of source) console.log("unexpected");
    }
    async runAsync(): Promise<void> {
        for await (super.item of [5, 6]) console.log("async", this.value);
        for await (super["item"] of [7]) console.log("async computed", this.value);
    }
    static run(): void {
        for (super.item of [8, 9]) console.log("static", this.received);
    }
}
const value = new Derived();
value.run();
Derived.run();
console.log("static receiver", Base.received, Derived.received);
function* source(): Generator<number> {
    try { yield 10; yield 11; } finally { console.log("iterator closed"); }
}
try { value.reject(source()); } catch (error) { console.log("caught", (error as Error).message); }
value.runAsync().catch(error => console.log("failure", String(error)));
