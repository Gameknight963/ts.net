type XY = { x: number; y: number };
type X = { x: number };
type $Named = { $value: string };

function forward(n: number): number { return twice(n); }
function twice(n: number): number { return n * 2; }
function factorial(n: number): number {
    if (n <= 1) { return 1; } else { return n * factorial(n - 1); }
}
function write(p: X): void { p.x = 17; }
function identity(p: XY): XY { return p; }
function runLoops(n: number): number {
    let result: number = 0;
    for (let i = 0; i < n; i++) { result += i % 3; }
    while (result < 20) { result++; }
    return result;
}

console.log(forward(4), factorial(6), runLoops(10));
const p = { y: 4, x: 3, extra: "retained" };
const view: X = p;
write(p);
console.log(view.x, p.extra, identity(p) === p);
const erased: unknown = p;
const back = erased as XY;
back.y = 99;
console.log(p.y, back === p);
const a: any = p;
a.x = 20;
a.extra = "changed";
a.other = "extension";
console.log(p.x, p.extra, a.other, a.absent);
const boxed: any = 3;
const number: number = boxed;
console.log(number + 2, boxed + 5, boxed + " things");
console.log(boxed * 2, boxed < 4);
const flag: any = true;
console.log(flag && true, !flag);
console.log("abc".length, ("ab" + "cd").length);
console.log('single " quote', "double ' quote", "slash: \\", "line\nbreak", "tab\tend");
console.log(0 / 0, 1 / 0, -1 / 0, -0);
const other = { y: 99, x: 20, extra: "changed" };
console.log("a" === "a", 1 === 1, boxed === 3, p !== other);
console.log((1e3 + 2.5) / 2, 10 - 3 - 2);
console.log(false && (p.x = 0) === 0, p.x);
const $object: $Named = { $value: "dollar" };
console.log($object.$value);
{
    const p = "shadow";
    console.log(p);
}
console.log(p.x);
console.log(1 + 2 as number);
console.log(-boxed as number);
let assigned = 0;
console.log(-(assigned = 5), assigned);
console.log(.5, 1., 1.e2, 1e999, 1e-999);
console.log();
