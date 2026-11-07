/// <reference lib="esnext.disposable" />
function resource(name: string) {
    console.log("open", name);
    return {name, async [Symbol.asyncDispose]() {console.log("dispose start", this.name); await Promise.resolve(); console.log("dispose end", this.name);}};
}
async function early() {await using value = resource("return"); return 4;}
async function main() {
    {await using a = resource("a"), b = resource("b"); console.log("body");}
    console.log("after block");
    console.log("return", await early());
    {await using value = {[Symbol.dispose]() {console.log("sync fallback");}};}
    {await using empty = null, missing = undefined; console.log("nullish");}
    for (await using value of [resource("loop a"), resource("loop b")]) {console.log("loop", value.name); if(value.name === "loop a") continue; break;}
    try {await using value = {async [Symbol.asyncDispose]() {throw "cleanup";}}; throw "body";}
    catch(e) {const error = e as SuppressedError; console.log("suppressed", error.error, error.suppressed);}
    let index = 0;
    for (await using value = resource("classic loop"); index < 2; index++) {console.log("classic", index);}
    for await (await using value of [resource("async iteration")]) {console.log("async iteration body"); break;}
    const captured: any = {async [Symbol.asyncDispose]() {console.log("original method", this === captured);}};
    {await using value = captured; captured[Symbol.asyncDispose] = () => console.log("replacement");}
    {await using value = {[Symbol.asyncDispose]: undefined, [Symbol.dispose]() {console.log("missing async fallback"); return {then() {console.log("must not assimilate");}};}};}
    try {await using value = {[Symbol.asyncDispose]: 1, [Symbol.dispose]() {console.log("wrong fallback");}} as any;}
    catch(e) {console.log("invalid async", (e as Error).name);}
    try {
        using sync = {[Symbol.dispose]() {console.log("mixed sync cleanup"); throw "sync failure";}};
        await using first = {async [Symbol.asyncDispose]() {throw "first failure";}}, second = {async [Symbol.asyncDispose]() {throw "second failure";}};
        throw "original failure";
    } catch(e) {
        let value: any = e;
        while(value instanceof SuppressedError) {console.log("chain", value.error); value = value.suppressed;}
        console.log("chain original", value);
    }
    try {
        await using first = resource("before init failure"), invalid = 1 as any;
    } catch(e) {console.log("initializer failure", (e as Error).name);}
    {await using thenable: any = {[Symbol.asyncDispose]() {return {then(resolve: (v: unknown) => void) {console.log("disposal thenable"); resolve(undefined);}};}} as any;}
    const iterable: AsyncIterable<AsyncDisposable> = {[Symbol.asyncIterator]() {
        return {async next() {return {done: false, value: resource("iterator resource")};}, async return() {console.log("async iterator closed"); return {done: true, value: undefined};}};
    }};
    for await (await using value of iterable) {console.log("iterator body"); break;}
    class Connection implements AsyncDisposable {
        closed = false;
        constructor(public name: string) {}
        async [Symbol.asyncDispose]() {await Promise.resolve(); this.closed = true; console.log("class closed", this.name);}
    }
    const connection = new Connection("connection");
    {await using value = connection; console.log("class open", value.closed);}
    console.log("class after", connection.closed);
    try {
        await using earlier = resource("before getter failure");
        await using invalid = {get [Symbol.asyncDispose](): () => Promise<void> {throw "getter failure";}};
    } catch(e) {console.log("getter caught", e);}
    outer: for (let index = 0; index < 2; index++) {
        for (await using value of [resource("label" + index)]) continue outer;
    }
    switch (2) {
        case 2: {await using value = resource("switch"); console.log("switch body"); break;}
    }
    async function finalReturn() {
        try {await using value = resource("finally return"); return 9;}
        finally {console.log("outer finally");}
    }
    console.log("final return", await finalReturn());
    console.log("done");
}
main();
