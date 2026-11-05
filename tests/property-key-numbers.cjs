const fs = require("node:fs");
const bytes = Buffer.alloc(8);
let count = 0;
for (const line of fs.readFileSync(process.argv[2], "utf8").trimEnd().split(/\r?\n/)) {
    const [hex, actual, integer] = line.split("\t");
    bytes.writeBigUInt64LE(BigInt("0x" + hex));
    const value = bytes.readDoubleLE();
    const expected = String(value);
    if (actual !== expected) throw new Error("Numeric property key mismatch: " + hex + ": " + actual + " vs " + expected);
    if (Number(integer) !== (value | 0)) throw new Error("Integer conversion mismatch: " + hex + ": " + integer + " vs " + (value | 0));
    count++;
}
if (count !== 20000) throw new Error("Unexpected sample count: " + count);
console.log("PASS 20,000 numeric property keys and integer conversions match Node");
