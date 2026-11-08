/// <reference lib="esnext.iterator" />
export {};
const iterator = [1, 2].values();
const original = iterator.next;
let calls = 0;
iterator.next = function() {calls++; return original.call(this);};
console.log("override", Iterator.from(iterator).map(value => value * 2).toArray().join(","), calls);
const captured = [3, 4].values();
const helper = captured.map(value => value);
captured.next = function() {return {value: 99, done: false};};
console.log("captured", helper.toArray().join(","));
const live = [1, 2];
const read = live.values().map(value => {if (value === 1) live.push(3); return value;});
console.log("live", read.toArray().join(","));
console.log("strings", Iterator.from("a😀b").map(value => value).toArray().join("|"));
console.log("entries", [8, 9].entries().map(pair => pair.join(":")).toArray().join(","));
console.log("keys", [8, 9].keys().map(value => value).toArray().join(","));
const holes = [1, , 3];
console.log("holes", holes.values().map(value => String(value)).toArray().join(","));
const failing = Object.create(null, {length: {value: 2}, "0": {value: 1}, "1": {get() {throw new Error("read");}}});
const failed = Array.prototype.values.call(failing).map(value => value);
console.log("first", failed.next().value);
try {failed.next();} catch (error) {console.log("failure", (error as Error).message, failed.next().done);}
