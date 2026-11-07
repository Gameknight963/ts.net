function multiply(first: any, second: any) { return first * second; }
function chained(first: any, second: any) { return multiply(first, second); }
const arrow = (first: any, second: any) => first * second;
const methods = { multiply(first: any, second: any) { return first * second; }};
console.log("function", multiply(2n, 3n), chained(4n, 5n));
console.log("arrow", arrow(3n, 4n));
console.log("method", methods.multiply(4n, 5n));
function local(first: any, second: any) { const result = first * second; return result; }
console.log("local", local(5n, 6n));
console.log("numbers", multiply(2, 3), arrow(3, 4), methods.multiply(4, 5), local(5, 6));
var result = 1;
var result = (2n as any) * (3n as any);
console.log("redeclared", result);
