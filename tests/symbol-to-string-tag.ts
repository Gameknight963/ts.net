const tag = Object.prototype.toString;
console.log("own", tag.call({[Symbol.toStringTag]: "Record"}));
console.log("empty", tag.call({[Symbol.toStringTag]: ""}));
const ancestor = {[Symbol.toStringTag]: "Parent"};
console.log("inherited", tag.call(Object.create(ancestor)));
class Named { get [Symbol.toStringTag](): string { return "Named"; } }
console.log("class", tag.call(new Named()));
let reads = 0;
const getter = {get [Symbol.toStringTag]() { reads++; console.log("receiver", this === getter); return "Getter"; }};
console.log("getter", tag.call(getter), reads);
for (const value of [undefined, null, 1, true, {}, Symbol("tag")]) console.log("ignored", tag.call({[Symbol.toStringTag]: value}));
const array: any = []; array[Symbol.toStringTag] = "Sequence";
console.log("array", tag.call(array));
const callable: any = () => 1; callable[Symbol.toStringTag] = "Callable";
console.log("function", tag.call(callable));
try { tag.call({get [Symbol.toStringTag]() { throw "tag failure"; }}); } catch (error) { console.log("throw", error); }
for (const value of ["Custom", 1, undefined]) {
    const object = Object.create(Map.prototype, {[Symbol.toStringTag]: {value, configurable: true}});
    console.log("map override", tag.call(object));
}
console.log("json", JSON.stringify({[Symbol.toStringTag]: "Hidden", value: 1}));
