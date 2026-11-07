function sequence(limit: number): AsyncIterable<number> {
    return {[Symbol.asyncIterator]() {
        let index = 0;
        return {async next() { return {done: index >= limit, value: index++}; }, async return() { console.log("close", index); await 0; console.log("closed"); return {done: true, value: 0}; }};
    }};
}
async function run(): Promise<void> {
    let sum = 0; for await (const value of sequence(3)) sum += value;
    console.log("sum", sum);
    for await (const value of sequence(3)) { console.log("first", value); break; }
    try { for await (const value of sequence(3)) { throw "body"; } } catch (error) { console.log("caught", error); }
    const both: any = {[Symbol.iterator]() { throw "wrong protocol"; }, [Symbol.asyncIterator]() { return {next() { return Promise.resolve({done: true}); }}; }};
    for await (const value of both) console.log("wrong", value);
    console.log("precedence");
    for (const source of [{[Symbol.asyncIterator]: 1}, {[Symbol.asyncIterator]() { return 1; }}, {[Symbol.asyncIterator]() { return {next: 1}; }}, {[Symbol.asyncIterator]() { return {async next() { return 1; }}; }}]) {
        try { for await (const value of source as any) console.log("wrong", value); } catch (error) { console.log("invalid", (error as Error).name); }
    }
    const rejected: any = {[Symbol.asyncIterator]() { return {next() { return Promise.reject("next failure"); }, return() { console.log("wrong close"); return {}; }}; }};
    try { for await (const value of rejected) console.log(value); } catch (error) { console.log("rejected", error); }
    const fromAsync: any = (Array as any).fromAsync;
    console.log("from async", (await fromAsync(sequence(3))).join(","));
    console.log("mapped", (await fromAsync(sequence(3), async (value: number, index: number) => value * 2 + index)).join(","));
    try { await fromAsync(sequence(3), () => { throw "map failure"; }); } catch (error) { console.log("map caught", error); }
    const wrapped: any = {[Symbol.asyncIterator]() { let done = false; return {async next() { if (done) return {done: true}; done = true; return {done: false, value: Promise.resolve(8)}; }}; }};
    for await (const value of wrapped) console.log("raw async value", typeof value.then, await value);
    console.log("done");
}
run();
