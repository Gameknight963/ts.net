/// <reference lib="esnext.disposable" />
function resource(name: string): Disposable {
    console.log("open", name);
    return { [Symbol.dispose]() { console.log("close", name); } };
}
let i = 0;
for (using r = resource("normal"); i < 3; i++) {
    console.log("body", i);
    if (i === 1) continue;
}
for (using r = resource("zero"); false;) console.log("unreachable");
for (using a = resource("a"), b = resource("b");;) { break; }
for (using empty = null, absent = undefined; false;) console.log("unreachable null");
function test() {
    for (using r = resource("return");;) return 42;
}
console.log(test());
outer: for (let n = 0; n < 2; n++) {
    for (using r = resource("label");;) continue outer;
}
try {
    for (using r = resource("throw");;) throw new Error("body");
} catch (e) { console.log((e as Error).message); }
for (using a = resource("outer");;) {
    for (using b = resource("inner");;) break;
    break;
}

function fail(message: string): never { throw new Error(message); }
try {
    for (using r = resource("condition"); fail("condition error");) {}
} catch (e) { console.log((e as Error).message); }
try {
    for (using r = resource("update"); true; fail("update error")) { console.log("update body"); }
} catch (e) { console.log((e as Error).message); }
try {
    for (using a = resource("before failure"), b = fail("initializer error");;) {}
} catch (e) { console.log((e as Error).message); }
let captured: (() => void) | undefined;
for (using r = resource("captured");;) {
    captured = () => console.log("captured resource", typeof r[Symbol.dispose]);
    break;
}
captured!();
async function asyncLoop() {
    for (using r = resource("async");;) {
        await Promise.resolve();
        console.log("async body");
        break;
    }
}
asyncLoop();
