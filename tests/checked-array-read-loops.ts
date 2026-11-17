export {};
function read(values: any[], index: number): any {return values[index];}
function sum(values: any[], count: number): number {
 let total = 0;
 for (let index = 0; index < count; index++) total += read(values, 0) as number;
 return total;
}
const ordinary: any[] = [7, "text"];
const tagged: (number | string)[] = [11, "text"];
console.log("tag", typeof tagged[0]);
console.log("ordinary", sum(ordinary, 64), sum(tagged, 64));
ordinary[0] = 13;
tagged[0] = 17;
console.log("updated", sum(ordinary, 64), sum(tagged, 64));
let gets = 0;
const accessor: any[] = [1, "text"];
Object.defineProperty(accessor, "0", {configurable: true, get() {return ++gets;}});
console.log("accessor", sum(accessor, 64), gets);
const changing: any[] = [1, "text"];
Object.defineProperty(changing, "0", {configurable: true, get() {
 Object.defineProperty(changing, "0", {value: 23, configurable: true, writable: true}); return 1;
}});
console.log("changing accessor", sum(changing, 64));
const inherited: any[] = [, "text"];
const prototype = Object.create(Array.prototype);
let inheritedGets = 0;
Object.defineProperty(prototype, "0", {get() {inheritedGets++; return 29;}});
Object.setPrototypeOf(inherited, prototype);
console.log("inherited", sum(inherited, 64), inheritedGets);
console.log("frozen", sum(Object.freeze([31, "text"]) as any, 64));
console.log("numeric representation", sum([37, 41], 64));
const materialized: (number | string)[] = ["text", 43];
console.log("materialized tag", typeof materialized[1]);
materialized.reverse();
console.log("materialized", sum(materialized, 64));
for (const value of [undefined, NaN, -0, Infinity]) {
 ordinary[0] = value;
 console.log("special", sum(ordinary, 64), 1 / sum(ordinary, 64));
}
console.log("empty", sum(null as any, 0), sum(undefined as any, 0), gets);
try {sum(null as any, 64);} catch (error) {console.log("null", (error as Error).name);}
function pending(count: number): number {
 let total = 0;
 for (let index = 0; index < count; index++) total += read(later, 0) as number;
 return total;
}
console.log("empty before initialization", pending(0));
try {pending(64);} catch (error) {console.log("pending", (error as Error).name);}
const later: any[] = [47, "text"];
console.log("initialized", pending(64));
function captured(): void {
 function work(count: number): number {
  let total = 0;
  for (let index = 0; index < count; index++) total += read(values, 0) as number;
  return total;
 }
 console.log("captured empty", work(0));
 try {work(64);} catch (error) {console.log("captured pending", (error as Error).name);}
 const values: any[] = [53, "text"];
 console.log("captured initialized", work(64));
}
captured();
let effects = 0;
function effectIndex(): number {effects++; return 0;}
function withEffects(): number {
 let total = 0;
 for (let index = 0; index < 64; index++) total += read(tagged, effectIndex()) as number;
 return total;
}
console.log("index effects", withEffects(), effects);
