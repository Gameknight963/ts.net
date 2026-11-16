export {};
class Counter {
    #value = 0;
    #key = "";
    static #last = 0;
    set #sink(value: number) { this.#value = value; console.log("setter", value); }
    run(): void {
        for (this.#value of [1, 2, 3]) console.log("value", this.#value);
        for (this.#key in { first: 1, second: 2 }) console.log("key", this.#key);
        for (Counter.#last of [4, 5]) console.log("static", Counter.#last);
        for (this.#sink of [6, 7]) console.log("stored", this.#value);
        console.log("public keys", Object.keys(this).join(","));
    }
    assign(target: Counter, source: Iterable<number>): void {
        for (target.#value of source) console.log("assigned", target.#value);
    }
    assignFrom(target: () => Counter, source: Iterable<number>): void {
        for (target().#value of source) console.log("ordered value", this.#value);
    }
    assignThrowing(source: Iterable<number>): void {
        for (this.#reject of source) console.log("unexpected");
    }
    set #reject(value: number) { throw new Error("setter " + value); }
    async runAsync(): Promise<void> {
        for await (this.#value of [Promise.resolve(8), Promise.resolve(9)]) console.log("async", this.#value);
        for await (this.#sink of [10]) console.log("async stored", this.#value);
        console.log("async public keys", Object.keys(this).join(","));
    }
    async assignAsync(target: Counter, source: AsyncIterable<number>): Promise<void> {
        for await (target.#value of source) console.log("async assigned", target.#value);
    }
}
const counter = new Counter();
counter.run();
let closed = 0;
function* source(): Generator<number> {
    try { yield 11; yield 12; } finally { closed++; }
}
try { counter.assign({} as Counter, source()); } catch (error) {
    console.log("wrong brand", (error as Error).name, closed);
}
try { counter.assignThrowing(source()); } catch (error) {
    console.log("throwing setter", (error as Error).message, closed);
}
function* ordered(): Generator<number> {
    console.log("next first"); yield 13;
    console.log("next second"); yield 14;
    console.log("done");
}
counter.assignFrom(() => { console.log("target"); return counter; }, ordered());
async function main(): Promise<void> {
    await counter.runAsync();
    async function* asynchronous(): AsyncGenerator<number> {
        try { yield 15; } finally {
            await Promise.resolve();
            console.log("async closed");
        }
    }
    try { await counter.assignAsync({} as Counter, asynchronous()); } catch (error) {
        console.log("async wrong brand", (error as Error).name);
    }
}
main().catch(error => console.log("failure", String(error)));
