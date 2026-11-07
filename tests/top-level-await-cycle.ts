export {};
const module = await import("./top-level-await-cycle-a.js");
console.log("ready", module.value);
