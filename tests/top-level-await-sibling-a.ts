import {gate} from "./top-level-await-gate.js";
console.log("sibling a start");
await gate;
export const value = 1;
console.log("sibling a end");
