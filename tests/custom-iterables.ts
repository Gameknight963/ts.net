function range(limit: number): Iterable<number> {
    return { [Symbol.iterator]() {
        let index = 0;
        return { next() { return {value: index++, done: index > limit}; }, return() { console.log("closed", index); return {done: true, value: 0}; } };
    } };
}
const values = range(3);
let sum = 0; for (const value of values) sum += value;
console.log("loop", sum);
console.log("spread", [...values].join(","));
console.log("from", Array.from(values).join(","));
console.log("set", [...new Set(values)].join(","));
const entries: any = { [Symbol.iterator]() { let i = 0; return {next() { return i++ === 0 ? {done: false, value: ["a", 7]} : {done: true}; }}; }};
console.log("map", new Map(entries).get("a"), Object.fromEntries(entries).a);
for (const value of values) { console.log("break", value); break; }
const [first, second] = values; console.log("destructure", first, second);
const iterable: any = {count: 0, [Symbol.iterator]() { console.log("receiver", this === iterable); return { next() { return {done: true}; } }; }};
console.log("empty", Array.from(iterable).length);
for (const value of [{[Symbol.iterator]: 1}, {[Symbol.iterator]() { return 1; }}, {[Symbol.iterator]() { return {next: 1}; }}]) {
    try { Array.from(value as any); } catch (error) { console.log("invalid", (error as Error).name); }
}
Promise.all(values).then(result => console.log("promise", result.join(",")));
let starts = 0;
const getterIterable: any = { get [Symbol.iterator]() { starts++; return function(this: any) { let done = false; return {next() { if (done) return {done: true}; done = true; return {done: false, value: 12}; }}; }; }};
console.log("getter", Array.from(getterIterable).join(","), starts);
async function consume() {
    const result: number[] = [];
    for await (const value of range(2)) result.push(value);
    console.log("async loop", result.join(","));
}
consume();
