import {value} from "./top-level-await-cycle-a.js";
export function read(): number {return value;}
console.log("cycle dependency");
