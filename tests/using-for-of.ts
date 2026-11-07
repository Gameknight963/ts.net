/// <reference lib="esnext.disposable" />
function resource(name: string) {
    console.log("open", name);
    return { name, [Symbol.dispose]() { console.log("close", name); } };
}
for (using value of [resource("a"), resource("b")]) console.log("body", value.name);
for (using value of [null, undefined]) console.log("empty", value);
for (using value of [resource("continue"), resource("break")]) {
    if (value.name === "continue") continue;
    break;
}
const captured: (() => string)[] = [];
for (using value of [resource("captured a"), resource("captured b")]) captured.push(() => value.name);
console.log(captured.map(f => f()).join(","));
function early() { for (using value of [resource("return")]) return value.name; }
console.log(early());
function values(): Iterable<Disposable> {
    return {[Symbol.iterator]() {
        let i = 0;
        return {next() { return {done: i++ > 1, value: resource("iterator" + i)}; }, return() { console.log("iterator closed"); return {done: true, value: undefined}; }};
    }};
}
for (using value of values()) { console.log("iterator body"); break; }
try { for (using value of values()) throw "body error"; } catch(e) { console.log(e); }
async function work() {
    for await (using value of [resource("async a"), resource("async b")]) {
        await Promise.resolve();
        console.log("async body", value.name);
    }
}
try {
    for (using value of [{[Symbol.dispose]() {console.log("dispose failure"); throw "dispose";}}]) throw "body";
} catch(e) {const error = e as SuppressedError; console.log("suppressed", error.error, error.suppressed);}
try {
    for (using value of [1 as any]) console.log("invalid body");
} catch(e) { console.log("invalid", (e as Error).name); }
outer: for (let i = 0; i < 2; i++) {
    for (using value of [resource("label" + i)]) continue outer;
}
async function customAsync() {
    const iterable: AsyncIterable<Disposable> = {[Symbol.asyncIterator]() {
        let done = false;
        return {async next() { if(done) return {done: true, value: undefined}; done = true; return {done: false, value: resource("custom async")}; },
            async return() {console.log("async iterator closed"); return {done: true, value: undefined}; }};
    }};
    for await (using value of iterable) {console.log("custom body"); break;}
}
async function main() { await work(); await customAsync(); }
main();

function failingValues(failOnLookup: boolean): Iterable<Disposable> {
    return {[Symbol.iterator]() {
        return {
            next() {return {done: false, value: {
                get [Symbol.dispose]() {
                    if (failOnLookup) throw "lookup failure";
                    return () => {throw "cleanup failure";};
                }
            }};},
            return() {console.log("failure iterator closed"); return {done: true, value: undefined};}
        };
    }};
}
for (const lookup of [true, false]) {
    try {for (using value of failingValues(lookup)) break;}
    catch (error) {console.log("iterator failure", error);}
}
