import {read} from "./top-level-await-var-b.js";
export var value: number;
console.log("var before", read());
await Promise.resolve();
value = 7;
console.log("var after", read());
