export {};
const input = [7];
let calls = 0;
Object.defineProperty(Array.prototype, "0", {
    configurable: true,
    get() { return 99; },
    set(value: number) { calls++; }
});
const plain = [1];
const head = [2, ...input];
const tail = [...input, 3];
const mixed: (number | string)[] = [4, "text"];
const reference: (number | string)[] = ["text", 5];
if (typeof mixed[0] !== "number" || typeof reference[1] !== "number") throw "wrong type";
const mixedValue = mixed[0], referenceValue = reference[0];
const missing: (number | undefined)[] = [undefined];
const holes: (number | undefined)[] = [,];
const missingOwn = Object.hasOwn(missing, "0"), holeOwn = Object.hasOwn(holes, "0");
const missingValue = missing[0], holeValue = holes[0];
const plainValue = plain[0], headValue = head[0], tailValue = tail[0];
const plainOwn = Object.hasOwn(plain, "0"), headOwn = Object.hasOwn(head, "0");
delete (Array.prototype as any)[0];
console.log("literals", calls, plainValue, headValue, tailValue, plainOwn, headOwn);
console.log(plain.join(","), head.join(","), tail.join(","));
console.log("mixed", mixedValue, referenceValue, mixed.join(","), reference.join(","));
console.log("presence", missingOwn, holeOwn, missingValue, holeValue);
Object.defineProperty(Array.prototype, "0", { configurable: true, value: 40, writable: false });
const readonlyNumber = [6];
const readonlyReference: (number | string)[] = ["reference", 8];
if (typeof readonlyReference[1] !== "number") throw "wrong type";
const readonlySpread = [9, ...input];
delete (Array.prototype as any)[0];
console.log("readonly prototype", readonlyNumber[0], readonlyReference[0], readonlySpread.join(","));
