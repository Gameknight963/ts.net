export {};
function right(value: any, number: number): any { return value + number; }
function left(number: number, value: any): any { return number + value; }
const values: any[] = [3, "4", null, undefined, true, false, -0, NaN, Infinity, Symbol("x"), 3n];
for (const value of values) {
    for (const number of [7, -0, undefined as unknown as number]) {
        for (const direction of [false, true]) {
            try { const result = direction ? left(number, value) : right(value, number); console.log(direction, String(result), typeof result, Object.is(result, -0)); }
            catch { console.log(direction, "throws"); }
        }
    }
}
const log: string[] = [];
function dynamic(): any { log.push("dynamic"); return { [Symbol.toPrimitive](hint: string) { log.push(hint); return "x"; } }; }
function numeric(): number { log.push("number"); return 7; }
console.log("right order", right(dynamic(), numeric()), log.join(","));
log.length = 0;
console.log("left order", left(numeric(), dynamic()), log.join(","));
let conversions = 0;
const changing: any = { valueOf() { conversions++; return conversions === 1 ? 3 : "x"; } };
console.log("changes", right(changing, 7), left(7, changing), conversions);
const failure: any = { valueOf() { throw new Error("coercion"); } };
try { right(failure, 7); } catch (error) { console.log((error as Error).message); }

function checkedRight(value: any, number: number): number { return value + number; }
function checkedLeft(number: number, value: any): number { return number + value; }
for (const value of [3, null, undefined, true, false, -0, NaN, Infinity, { valueOf() { return 3; } }] as any[]) {
    for (const number of [7, -0, undefined as unknown as number]) {
        const a = checkedRight(value, number), b = checkedLeft(number, value);
        console.log("checked", String(a), String(b), Object.is(a, -0), Object.is(b, -0));
    }
}
