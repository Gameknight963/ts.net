console.log("failing module initialized");
export const value = 1;
throw new Error("module failure");
