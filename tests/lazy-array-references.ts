export {};
function read(values: any[], index: number): any { return values[index]; }
const values = [1, 2, 3].map(value => value * 2);
console.log("boxed", read(values, 0), read(values, 0));
values[0] = 9;
console.log("overwrite cache", read(values, 0));
const dynamic: any[] = values;
dynamic[1] = "text";
dynamic[2] = null;
console.log("references", JSON.stringify(dynamic));
dynamic[1] = 11;
dynamic[2] = undefined;
console.log("numbers and missing", JSON.stringify(dynamic), typeof dynamic[2]);
delete dynamic[0];
console.log("delete", 0 in dynamic, dynamic[0]);
dynamic[0] = 13;
dynamic.length = 1;
dynamic.length = 4;
console.log("resize", JSON.stringify(dynamic), 1 in dynamic, 2 in dynamic);

const growing = [1, 2, 3].map(value => value + 1);
for (let i = 0; i < 40; i++) growing.push(i);
console.log("grown", growing.length, read(growing, 0), read(growing, 42));
const references: any[] = growing;
const shared = { value: 17 };
references[20] = shared;
references.push("last");
console.log("after reference", references[20] === shared, references[43]);
references[20] = 19;
console.log("replace reference", read(references, 20));
console.log("reverse", references.reverse()[0], references[references.length - 1]);

const missing = [1, 2, 3].map(value => value === 2 ? undefined : value);
console.log("missing map", missing.map(value => value ?? 7).join(","));
console.log("freeze", Object.freeze([1, 2, 3].map(value => value * 2)).join(","));
