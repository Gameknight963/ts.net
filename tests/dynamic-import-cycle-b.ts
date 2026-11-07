import {value} from "./dynamic-import-cycle-a.js";
export function read() {return value;}
console.log("cycle b");
