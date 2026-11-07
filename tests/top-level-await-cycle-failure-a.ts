import {read} from "./top-level-await-cycle-failure-b.js";
export const value = 1;
console.log("cycle failure start", read());
await Promise.resolve();
throw new Error("cycle failure");
