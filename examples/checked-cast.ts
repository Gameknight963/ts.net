// Accepted by TypeScript, but this runtime catches the false assertion.
const value: unknown = "hello";
const count = value as number;
console.log(count + 1);
