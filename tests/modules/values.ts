console.log("values initialized");
export let count = 1;
export const shared = { x: 2 };
export function increment() { count++; shared.x++; }
export function read() { return count; }
export default function multiply(value: number) { return value * count; }
