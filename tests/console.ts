console.info("info", 3, true, undefined, null);
console.debug("debug", "text"); console.warn("warning",4);console.error("error",false);
console.assert(true,"hidden"); console.assert(false,"failed",42);console.assert(false);console.assert();
const error=console.error;error("detached",2);
for(const method of [console.log,console.info,console.debug,console.warn,console.error,console.assert]) console.log("metadata",method.name,method.length);
console.log("results",console.info(),console.error());

console.warn(...["spread",5]);
const {info}=console; info("destructured",6);
async function later() { console.error(await Promise.resolve("awaited")); }
later();
