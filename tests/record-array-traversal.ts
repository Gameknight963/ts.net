export {};
interface Point {x: number; y: number;}
function sum(items: Point[]): number {
 let total = 0;
 for (let i = 0; i < items.length; i++) total += items[i].x + items[i].y;
 return total;
}
const items: Point[] = [];
for (let i = 0; i < 64; i++) items.push({x: i, y: i + 1});
console.log("plain", sum(items), sum([]));
items[0].x = 100;
console.log("changed", sum(items));
let gets = 0;
const getter: Point = {x: 1, y: 2};
const changing: Point[] = [getter, {x: 3, y: 4}];
Object.defineProperty(getter, "x", {get() {
 gets++;
 changing[0] = {x: 10, y: 20};
 changing.push({x: 30, y: 40});
 return 1;
}});
console.log("getter changes array", sum(changing), gets, changing.length);
const indexed: Point[] = [{x: 1, y: 2}];
Object.defineProperty(indexed, "0", {get() {gets++; return {x: gets, y: gets};}});
console.log("indexed getter", sum(indexed), gets);
const sparse: Point[] = new Array(2);
sparse[1] = {x: 5, y: 6};
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, "0", {get() {gets++; return {x: gets, y: 7};}});
Object.setPrototypeOf(sparse, prototype);
console.log("inherited", sum(sparse), gets);
const frozen: Point[] = [{x: 8, y: 9}];
Object.freeze(frozen[0]); Object.freeze(frozen);
console.log("frozen", sum(frozen));
const missing = [{x: undefined as any, y: 1}];
console.log("undefined field", sum(missing));
console.log("nan field", sum([{x: NaN, y: 1}]));
console.log("null element");
try {console.log(sum([null as any]));} catch (error) {console.log((error as Error).name);}
let later: Point[];
console.log("undefined array");
try {console.log(sum(later!));} catch (error) {console.log((error as Error).name);}
function shifted(items: Point[]): number {
 let total = 0;
 for (let i = 3; i < items.length; i++) total += items[i].x - items[i].y * 2 + i;
 return total;
}
console.log("nonzero start", shifted(items), shifted([]));
