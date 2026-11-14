export {};
function fresh(): number[] { return [1, 2, 3, 4].map(value => value * 2); }
console.log("map", fresh().map(value => value + 1).join(","));
console.log("filter", fresh().filter(value => value > 4).join(","));
let sum = 0;
fresh().forEach((value, index, array) => { sum += value + index + array.length; });
console.log("forEach", sum);
console.log("predicates", fresh().every(value => value > 0), fresh().some(value => value === 6), fresh().findIndex(value => value === 6));
const missing = [1, 2, 3].map(value => value === 2 ? undefined : value);
console.log("missing", missing.map(value => value ?? 9).join(","), missing.filter(value => value === undefined).length);
console.log("find missing", missing.findIndex(value => value === undefined));
for (const mode of ["getter", "delete", "shrink", "reverse", "prototype"]) {
    const values = fresh(); let calls = 0, gets = 0;
    const result = values.map(value => {
        if (++calls === 1) {
            if (mode === "getter") Object.defineProperty(values, "1", { get() { gets++; return 20; } });
            if (mode === "delete") delete values[1];
            if (mode === "shrink") values.length = 1;
            if (mode === "reverse") values.reverse();
            if (mode === "prototype") {
                delete values[1];
                const parent = Object.create(Array.prototype);
                Object.defineProperty(parent, "1", { get() { gets++; return 30; } });
                Object.setPrototypeOf(values, parent);
            }
        }
        return value + 1;
    });
    console.log(mode, result.join(","), calls, gets);
}
console.log("frozen", Object.freeze(fresh()).filter(value => value > 2).join(","));
