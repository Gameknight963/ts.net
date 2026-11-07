/// <reference lib="esnext.disposable" />
export let closed = false;
using held = {[Symbol.dispose]() {closed = true; console.log("dependency closed");}};
export function readClosed(): boolean {return closed;}
console.log("dependency body", closed);
