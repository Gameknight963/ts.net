function countNumbers(values: (number | string)[]): number {
    let total = 0;
    for (let i = 0; i < values.length; i++) {
        const value = values[i];
        if (typeof value === "number") total += value;
        else console.log(value.length);
    }
    return total;
}
const mixed: (number | string)[] = [];
for (let i = 0; i < 6; i++) mixed[i] = i % 2 === 0 ? i : "text";
console.log(countNumbers(mixed));
const alias = mixed;
alias[0] = "changed";
alias[1] = 12;
console.log(countNumbers(mixed));
console.log(mixed[20], typeof mixed[20]);
console.log(countNumbers(mixed.slice(1, 4)));
mixed.reverse();
console.log(countNumbers(mixed), mixed[0], mixed[5]);

function classify(values: (number | string)[], count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) {
        if (typeof values[i] === "number") total += 1;
        if ("number" !== typeof values[i]) total += 10;
    }
    return total;
}
const taggedValues: (number | string)[] = [];
for (let i = 0; i < 6; i++) taggedValues[i] = i % 2 === 0 ? i + 0.5 : "text";
console.log(classify(taggedValues, 6), classify(taggedValues, 9));
const taggedAlias = taggedValues;
taggedAlias[1] = 5;
console.log(classify(taggedValues, 6));
console.log(taggedValues[0]); // Reading payloads can change the backing store.
console.log(classify(taggedAlias, 6));
taggedValues.length = 1;taggedValues.length = 6;
console.log(classify(taggedValues, 6), Object.hasOwn(taggedValues, 1));
let storeTarget: any[] = [];
const storeOriginal = storeTarget;
function replaceStoreTarget(): number { storeTarget = [];return 7; }
for (let i = 0; i < 1; i++) storeTarget[i] = i === 0 ? replaceStoreTarget() : "text";
console.log(storeOriginal[0], storeTarget[0]);

function makeScannedArray(): (number | string)[] {
    const values: (number | string)[] = [];
    for (let i = 0; i < 8; i++) values[i] = i % 2 === 0 ? i : "text";
    let found = 0;
    for (let i = 0; i < 8; i++) if (typeof values[i] === "number") found++;
    if (found !== 4) console.log("wrong count");
    return values;
}
function makePayloadArray(): (number | string)[] { return [1, "text"]; }
console.log(makeScannedArray().length, makePayloadArray().length);
