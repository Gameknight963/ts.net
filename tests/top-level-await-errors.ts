export {};
let first: unknown;
try {await import("./top-level-await-failure.js");} catch(error) {first = error; console.log("failure", (error as Error).message);}
try {await import("./top-level-await-failure.js");} catch(error) {console.log("cached", error === first);}
console.log("entry survived");

let cycleFailure: unknown;
try {await import("./top-level-await-cycle-failure-a.js");} catch(error) {cycleFailure = error; console.log("cycle failure", (error as Error).message);}
try {await import("./top-level-await-cycle-failure-b.js");} catch(error) {console.log("cycle failure shared", error === cycleFailure);}
