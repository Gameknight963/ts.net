/// <reference lib="esnext.disposable" />
let closed = false;
using value = {[Symbol.dispose]() {closed = true; console.log("module disposed");}};
export function isClosed() {return closed;}
console.log("module using body");
