const first = Symbol("first"), second = Symbol("first");
const map = new WeakMap<any, any>([[first, "one"]]);
console.log("map", map.get(first), map.has(first), map.has(second));
console.log("chain", map.set(second, "two") === map, map.get(second));
console.log("delete", map.delete(first), map.delete(first), map.get(first));
const set = new WeakSet<any>([first, second]);
console.log("set", set.has(first), set.has(second), set.add(first) === set, set.delete(first), set.has(first));
const reference = new WeakRef(first as any);
console.log("reference", reference.deref() === first);
for (const key of [Symbol.for("registered"), 1, "text", null, undefined]) {
    console.log("query", map.has(key), map.get(key), map.delete(key), set.has(key), set.delete(key));
    for (const operation of [() => map.set(key, 1), () => set.add(key), () => new WeakRef(key as any)]) {
        try { operation(); } catch (error) { console.log("invalid", (error as Error).name); }
    }
}
console.log("well known", new WeakMap<any, number>([[Symbol.iterator, 7]]).get(Symbol.iterator));
const box = Object(first);
console.log("boxed", new WeakSet<any>([box]).has(box), new WeakSet<any>([box]).has(first));
