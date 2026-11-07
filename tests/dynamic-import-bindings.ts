import * as initial from "./dynamic-import-values.js";
import {Library} from "./namespace-helper.js";
import {run as cleanup, closed} from "./await-using-helper.js";
let total = 5;
function counter() {let value = 0; return () => ++value;}
const increment = counter();
class Box {
    #value = total;
    read() {return this.#value;}
}
namespace Box {export function create() {return new Box();}}
const callbacks: (() => number)[] = [];
for (let index = 0; index < 3; index++) callbacks.push(() => index);
console.log("bindings", increment(), increment(), Box.create().read(), callbacks.map(f => f()).join(","), Library.increment());
async function run() {
    const loaded = await import("./dynamic-import-values.js");
    console.log("static identity", loaded === initial, loaded.bump(), initial.value);
    const read = await cleanup();
    console.log("cleanup", closed, read());
    total = 8;
    console.log("updated", Box.create().read());
}
run();
