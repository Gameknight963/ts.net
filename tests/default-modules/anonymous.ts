import { early } from "./anonymous-cycle.js";
export default function(value: number) { return value * 2; }
console.log("anonymous", early);
