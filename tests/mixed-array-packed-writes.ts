export {};

function write(values: (number | string | undefined)[], count: number): void {
    for (let index = 0; index < count; index++) values[0] = index + 0.5;
}
function masked(values: (number | string)[], count: number): void {
    for (let index = 0; index < count; index++) values[index & 1] = index;
}
const values: (number | string | undefined)[] = [7, "text", undefined];
console.log("cached", values[0]);
write(values, 8);
console.log("written", values.join(","), typeof values[0], values[2] === undefined);
const prefix: (number | string)[] = [1, 2, "tail"];
masked(prefix, 8);
console.log("prefix", prefix.join(","));
const mixed: (number | string)[] = [1, "text"];
masked(mixed, 8);
console.log("mixed fallback", mixed.join(","));
for (const count of [0, -1, NaN, 0.5, 2.5]) {
    const items: (number | string | undefined)[] = [3, "tail"];
    write(items, count);
    console.log("count", count, items.join(","));
}
const hole: (number | string | undefined)[] = [, "tail"];
write(hole, 2);
console.log("hole", hole.join(","), 0 in hole);
const frozen: (number | string | undefined)[] = [9, "tail"];
Object.freeze(frozen);
try { write(frozen, 2); } catch (error) { console.log("frozen", (error as Error).name, frozen[0]); }
const accessor: (number | string | undefined)[] = [1, "tail"];
let writes = 0;
Object.defineProperty(accessor, "0", { get() { return 17; }, set(value) { writes += value; } });
write(accessor, 3);
console.log("accessor", writes, accessor[0]);
const reentered: (number | string | undefined)[] = [1, "tail"];
const source = { get value(): number { reentered.length = 0; return 19; } };
for (let index = 0; index < 3; index++) reentered[0] = source.value;
console.log("getter", reentered.length, reentered[0]);
const compound: any[] = ["x", "tail"];
for (let index = 0; index < 3; index++) compound[0] += index;
console.log("compound", compound.join(","));
let assigned: number | string = "initial";
for (let index = 0; index < 3; index++) assigned = (compound[0] = index);
console.log("assignment value", assigned, compound.join(","));
for (let index = 0; index < 3; index++) values[0] = -0;
console.log("negative zero", Object.is(values[0], -0));
for (let index = 0; index < 3; index++) values[0] = 0 / 0;
console.log("nan", Number.isNaN(values[0]));
for (let index = 0; index < 3; index++) values[0] = 1 / 0;
console.log("infinity", values[0]);
for (let index = 0; index < 3; index++) values[0] = undefined as unknown as number;
console.log("undefined assertion", values[0] === undefined);
const inherited: (number | string | undefined)[] = [, "tail"];
let inheritedWrites = 0;
Object.setPrototypeOf(inherited, { set 0(value: number) { inheritedWrites += value; } });
write(inherited, 3);
console.log("inherited setter", inheritedWrites, Object.hasOwn(inherited, "0"));
