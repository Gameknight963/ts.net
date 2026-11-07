export {};
console.log("async failure start");
await Promise.resolve();
throw new Error("async module failure");
