function invoke(): boolean {
    const copy = original;
    return copy(2);
}
console.log(invoke());
const original = isNumber;
function isNumber(value: number): boolean { return typeof value === "number"; }
