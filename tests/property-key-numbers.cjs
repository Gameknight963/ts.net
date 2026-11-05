const fs = require("node:fs");
const bytes = Buffer.alloc(8);
let count = 0;
for (const line of fs.readFileSync(process.argv[2], "utf8").trimEnd().split(/\r?\n/)) {
    const [hex, actual] = line.split("\t");
    bytes.writeBigUInt64LE(BigInt("0x" + hex));
    const value = bytes.readDoubleLE();
    const expected = String(value);
    if (actual !== expected) throw new Error("Numeric property key mismatch: " + hex + ": " + actual + " vs " + expected);
    count++;
}
if (count !== 20000) throw new Error("Unexpected sample count: " + count);
console.log("PASS 20,000 numeric property keys match Node");
