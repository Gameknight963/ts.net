import {read} from "./dynamic-import-cycle-b.js";
export const value = "cycle value";
console.log("cycle a", read());
export {read};
