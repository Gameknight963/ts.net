function isNumber(value: number): boolean { return typeof value === "number"; }
const first = isNumber;
const second = first;
const third = second;
console.log(third(2), third(NaN), third.name, first === third);
const dynamic: any = third;
console.log(dynamic(), dynamic(...[]), dynamic(2, 3));
let mutable = isNumber;
const saved = mutable;
mutable = value => false;
console.log(mutable(2), saved(2));
function opposite(value: number): boolean { return typeof value !== "number"; }
function defaulted(check = isNumber): boolean { return check(2); }
console.log(defaulted(), defaulted(opposite));
function local(count: number, seed: number): number {
    const root = isNumber;
    const forwarded = root;
    let total = 0;
    for (let i = 0; i < count; i++) if (forwarded(i)) total += 1;
    return total + seed;
}
console.log(local(8, 7));
let trace = "";
function mark(value: number): number { trace += value;return value; }
console.log(dynamic(mark(1), mark(2)), trace);
console.log(dynamic(...[mark(3), mark(4)]), trace);
