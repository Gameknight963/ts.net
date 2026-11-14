export {};

function score(value: any): number {
    return typeof value === "number" ? value : typeof value === "string" ? value.length : 0;
}
const inherited: any[] = [1, , undefined, "four"];
let reads = 0;
const parent = Object.create(Array.prototype);
Object.defineProperty(parent, "1", { get() { reads++; return "two"; } });
Object.setPrototypeOf(inherited, parent);
console.log("inherited", inherited.map(score).join(","), reads);
const holes: any[] = [1, , undefined, "four"];
let visits = 0;
console.log("holes", holes.map((value: any) => { visits++; return score(value); }).join(","), visits);
console.log("filter", holes.filter((value: any) => score(value) > 0).join(","));
console.log("every", holes.every((value: any) => value !== "missing"));
console.log("some", holes.some((value: any) => typeof value === "string"));

for (const mode of ["getter", "delete", "shrink", "reverse", "prototype"]) {
    const values: any[] = [1, "two", 3, "four"];
    let calls = 0;
    let gets = 0;
    const result = values.map((value: any): number => {
        if (++calls === 1) {
            if (mode === "getter") Object.defineProperty(values, "1", { get() { gets++; return "changed"; } });
            if (mode === "delete") delete values[1];
            if (mode === "shrink") values.length = 1;
            if (mode === "reverse") values.reverse();
            if (mode === "prototype") {
                delete values[1];
                const prototype = Object.create(Array.prototype);
                Object.defineProperty(prototype, "1", { get() { gets++; return "changed"; } });
                Object.setPrototypeOf(values, prototype);
            }
        }
        return score(value);
    });
    console.log(mode, result.join(","), calls, gets);
}
console.log("frozen", Object.freeze([1, "two", undefined]).map(score).join(","));
const sparse: any[] = [1]; sparse[1000] = "four";
let sum = 0;
sparse.forEach((value: any) => { sum += score(value); });
console.log("sparse", sum);
