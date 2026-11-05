function tally(values: (number | string)[], count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) if (typeof values[i] === "number") total += 1;
    return total;
}
const mixed: (number | string)[] = [1, "text", 2, "more"];
const texts: (number | string)[] = ["a", "b", "c", "d"];
const missing = undefined as unknown as number;
console.log(tally(mixed, 4, 7), tally(mixed, 4, 0.25), tally(mixed, 4, -3));
console.log(tally(mixed, 4, missing), tally(texts, 4, missing), tally(mixed, 0, missing));
console.log(tally(mixed, 7, 2), tally(mixed, -1, missing), tally(mixed, 1.5, 3));
console.log(tally(mixed, 4, NaN), tally(mixed, 4, Infinity), tally(mixed, 4, -Infinity));
console.log(1 / tally(texts, 4, -0), tally(mixed, 4, -0));
function leaveEarly(values: (number | string)[], seed: number): number {
    let total = seed;
    for (let i = 0; i < 4; i++) {
        if (typeof values[i] === "number") total += 1;
        if (i === 1) break;
    }
    return total;
}
console.log(leaveEarly(mixed, 7), leaveEarly(texts, missing));
function overwrite(values: (number | string)[], seed: number): number {
    let total = seed;
    for (let i = 0; i < 4; i++) if (typeof values[i] === "number") total = seed;
    return total;
}
console.log(overwrite(mixed, missing));
