/// <reference lib="esnext.disposable" />
import {value,read} from "./top-level-await-helper.js";
console.log("entry", value, read());
export const result = await Promise.resolve(12);
console.log("resumed", result);
await using resource = {async [Symbol.asyncDispose]() {console.log("cleanup start"); await Promise.resolve(); console.log("cleanup end");}};
for await (const value of [Promise.resolve(1), Promise.resolve(2)]) console.log("iteration", value);
console.log("body end");

export let count = 0;
for await (await using value of [{async [Symbol.asyncDispose]() {count++; console.log("loop disposed");}}]) {
    await Promise.resolve(); console.log("loop body");
}
try {await Promise.reject("caught");} catch(error) {console.log(error);} finally {await Promise.resolve(); console.log("awaited finally");}
console.log("final count", count);

const {left, right} = await Promise.resolve({left: 2, right: 3});
let captured = left;
const increment = () => ++captured;
class Box {#value = left + right; read() {return this.#value;}}
namespace Box {export const create = () => new Box();}
console.log("bindings", increment(), increment(), Box.create().read());
