import { other } from "./cycle-b.js";
export function first() { return 3; }
export const cyclic = other() + 1;
