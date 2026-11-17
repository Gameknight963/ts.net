export {};
function read(values: any[], index: number): any {return values[index];}
const values: any[] = [];
values.push({x: 7, y: 11}, null, undefined, "text");
console.log("data", read(values, 0).x, read(values, -0).y, read(values, 1), read(values, 2), read(values, 3));
for (const index of [-1, 0.5, NaN, Infinity, -Infinity, 2147483648, 4294967295]) {
 (values as any)[index] = "key:" + index;
 console.log("key", index, read(values, index));
}
console.log("undefined key", read(values, undefined as any));
let reads = 0;
Object.defineProperty(values, "0", {configurable: true, get() {
 reads++;
 values[1] = {x: reads};
 return {x: reads};
}});
console.log("accessor", read(values, 0).x, read(values, 1).x, read(values, 0).x, reads);
Object.defineProperty(values, "0", {value: {x: 17}, configurable: true, writable: true});
console.log("restored", read(values, 0).x, reads);
delete values[0];
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, "0", {get() {return {x: ++reads};}});
Object.setPrototypeOf(values, prototype);
console.log("inherited", read(values, 0).x, read(values, 0).x, reads);
values.length = 0;
console.log("truncated", read(values, 1));
const frozen: any[] = [];
frozen.push({x: 19});
Object.freeze(frozen);
console.log("frozen", read(frozen, 0).x, read(frozen, 1));
const sparse: any[] = [];
sparse[100000] = {x: 23};
console.log("sparse", read(sparse, 100000).x, read(sparse, 99999));
const numbers = [29, 31];
console.log("numbers", read(numbers, 0), read(numbers, 1));
const mixed: (number | string)[] = [37, "text"];
console.log("tagged", typeof mixed[0], read(mixed, 0), read(mixed, 1));
