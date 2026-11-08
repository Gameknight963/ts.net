// Accepted by TypeScript, but ts.net catches the false assertion (by throwing an exception)
const value: unknown = "hello";
const count = value as number;
console.log(count + 1);
