console.log("dependency start");
export let value = await Promise.resolve(7);
export function read() {return value;}
console.log("dependency end", value);
