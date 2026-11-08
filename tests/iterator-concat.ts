/// <reference lib="esnext.iterator" />
export {};
// Generator delegation provides closing semantics; method lookup remains eager.
function oracle(...items: any[]): any {
 const sources = items.map(item => {
  if (item === null || typeof item !== "object" && typeof item !== "function") throw new TypeError();
  const method = item[Symbol.iterator]; if (typeof method !== "function") throw new TypeError();
  return {[Symbol.iterator]() {return method.call(item);}};
 });
 return (function*() {for (const source of sources) yield* source;})();
}
const concat: (...items: any[]) => any = (Iterator as any).concat ?? oracle;
console.log("values", Array.from(concat([1, 2], [], new Set([3, 4]), new Uint8Array([5]), new String("ab"))).join(","));
console.log("empty", concat().next().done);
let effects = "";
function source(label: string): any {
 return {get [Symbol.iterator]() {
  effects += "get" + label + ";";
  return function() {effects += "open" + label + ";"; let i = 0; return {
   next() {effects += "next" + label + ";"; return i++ ? {done: true} : {done: false, value: label};},
   return() {effects += "close" + label + ";"; return {done: true};}
  };};
 }};
}
const a = source("A"), b = source("B"), joined = concat(a, b);
console.log("eager", effects);
console.log("first", joined.next().value, effects);
console.log("second", joined.next().value, effects);
console.log("return", joined.return().value, joined.next().done, effects);
effects = ""; const unused = concat(source("X")); unused.return(); console.log("unopened", effects);
effects = ""; try {concat(source("Y"), {});} catch (error) {console.log("validate", (error as Error).name, effects);}
for (const value of [null, undefined, "abc", 3, {}, {next() {return {done:true};}}, {[Symbol.iterator]:null}, {[Symbol.iterator]:1}]) {
 try {concat(value); console.log("unexpected");} catch (error) {console.log("invalid", (error as Error).name);}
}
const bad = concat({[Symbol.iterator]() {return 1;}});
try {bad.next();} catch (error) {console.log("bad iterator", (error as Error).name, bad.next().done);}
let closed = 0;
function* values(): Generator<number> {try {yield 1; yield 2;} finally {closed++;}}
for (const value of concat(values(), [3])) {console.log("break", value); break;}
console.log("closed", closed);
console.log("helpers", concat([1, 2], [3, 4]).map((value: number) => value * 2).take(3).toArray().join(","));
const mutable: any = {[Symbol.iterator]: function*() {yield "original";}};
const captured = concat(mutable); mutable[Symbol.iterator] = function*() {yield "replacement";};
console.log("captured", captured.next().value);
const array = [1]; const live = concat(array); array.push(2);
console.log("live", Array.from(live).join(","));
let active: any;
const reentrant = {[Symbol.iterator]() {let count = 0; return {next() {
 if (count++) return {done: true};
 try {active.next();} catch (error) {console.log("reentrant next", (error as Error).name);}
 try {active.return();} catch (error) {console.log("reentrant return", (error as Error).name);}
 return {value: 7, done: false};
}};}};
active = concat(reentrant); console.log("reentrant result", active.next().value, active.next().done);
let badClosed = 0;
const throwsNext = concat({[Symbol.iterator]() {return {
 next() {throw new Error("next failed");}, return() {badClosed++;return {};}
};}});
try {throwsNext.next();} catch (error) {console.log("next failed", (error as Error).message, badClosed, throwsNext.next().done);}
const throwsReturn = concat({[Symbol.iterator]() {return {
 next() {return {value: 1, done: false};}, return() {throw new Error("return failed");}
};}});
throwsReturn.next();
try {throwsReturn.return();} catch (error) {console.log("return failed", (error as Error).message, throwsReturn.next().done);}
const invalidReturn = concat({[Symbol.iterator]() {return {next() {return {value: 1, done: false};}, return() {return 1;}};}});
invalidReturn.next();try {invalidReturn.return();} catch (error) {console.log("invalid return", (error as Error).name, invalidReturn.next().done);}
