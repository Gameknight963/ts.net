import {read} from "./top-level-await-cycle-b.js";
export let value = 0;
console.log("cycle start");
await Promise.resolve();
void import("./top-level-await-cycle-b.js").then(module => console.log("cycle import", module.read()));
await Promise.resolve();
await Promise.resolve();
value = 5;
console.log("cycle end", read());
