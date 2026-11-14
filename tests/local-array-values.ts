export {};
function dense(seed: number): number { const values = [seed, seed + 1]; return values[0] + values[1]; }
console.log("dense", dense(3), dense(1.5), dense(Infinity), dense(NaN));
function zero(): number { const values = [-0, -0]; return values[0] + values[1]; }
console.log("zero", Object.is(zero(), -0));
function maybe(flag: boolean): number { return flag ? 3 : undefined as any; }
function missing(flag: boolean): number { const values = [maybe(flag), 2]; return values[0] + values[1]; }
console.log("missing", missing(true), missing(false));
let events = "";
function next(value: number): number { events += value; return value; }
function ordered(): number { const values = [next(1), next(2)]; events += "after"; return values[0] + values[1]; }
console.log("order", ordered(), events);
function shadow(): number {
    const values = [1, 2]; let total = values[0] + values[1];
    { const values = [3, 4]; total += values[0] + values[1]; }
    return total + values[0] + values[1];
}
console.log("shadow", shadow());
function branch(flag: boolean): number { const values = [1, 2]; if (flag) return values[0] + values[1]; return values[1] + values[0]; }
console.log("branch", branch(false), branch(true));
function alias(): number { const values = [1, 2]; const other = values; other[0] = 7; return values[0] + values[1]; }
function escape(): number[] { const values = [1, 2]; return values; }
function mutate(): number { const values = [1, 2]; values[0]++; return values[0] + values[1]; }
function dynamic(index: number): number { const values = [1, 2]; return values[index] + values[1]; }
function length(): number { const values = [1, 2]; return values[0] + values.length; }
function capture(): number { const values = [1, 2]; const change = () => { values[0] = 7; }; change(); return values[0] + values[1]; }
function captureRead(): number { const values = [1, 2]; return (() => values[0] + values[1])(); }
function getter(): number { const values = [1, 2]; Object.defineProperty(values, "0", { get() { return 7; } }); return values[0] + values[1]; }
function holes(): number { const values = [1, , 3]; return values[0]! + values[2]!; }
function spread(): number { const input = [2]; const values = [1, ...input]; return values[0] + values[1]; }
console.log("fallbacks", alias(), escape().join(","), mutate(), dynamic(0), length(), capture(), captureRead(), getter(), holes(), spread());
function forward(): number {
    try {
        // @ts-ignore intentional read before initialization
        return values[0] + values[1];
    } catch { return -1; }
    var values = [1, 2];
}
function self(): number {
    try {
        // @ts-ignore intentional self-reference
        const values: number[] = [values[0] + 1, 2];
        return values[0] + values[1];
    } catch { return -1; }
}
console.log("initialization", forward(), self());
function switchPath(mode: number): number {
    switch (mode) {
        case 0: var values = [1, 2];
        case 1:
            try {
                // @ts-ignore intentional case entry before initialization
                return values[0] + values[1];
            } catch { return -1; }
        default: return 0;
    }
}
function loop(): number { let total = 0; for (let i = 0; i < 3; i++) { const values = [i, i + 1]; if (i === 1) continue; total += values[0] + values[1]; } return total; }
console.log("control", switchPath(0), switchPath(1), loop());
namespace Local { const values = [1, 2]; export const sum = values[0] + values[1]; }
function environment(): number { let value = 1; const read = () => value; const values = [1, 2]; return values[0] + values[1] + read(); }
console.log("storage", Local.sum, environment());
let reads = 0;
Object.defineProperty(Array.prototype, "0", { configurable: true, get() { reads++; return 99; }, set() { reads++; } });
const inherited = dense(3);
delete (Array.prototype as any)[0];
console.log("inherited", inherited, reads);
async function asynchronous(): Promise<number> { const values = [1, 2]; await Promise.resolve(); return values[0] + values[1]; }
asynchronous().then(value => console.log("async", value));
function* generator() { const values = [1, 2]; yield 0; return values[0] + values[1]; }
const iterator = generator(); iterator.next(); console.log("generator", iterator.next().value);
