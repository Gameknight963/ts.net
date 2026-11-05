function step(value: number): number { return value + 1; }
function down(value: number): number { return value - 2; }
function kernel(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total = step(total);
    return total;
}
function descending(count: number, seed: number): number {
    let total = seed;
    for (let i = 2; i < count; i++) total = down(total);
    return total;
}
for (const count of [-1, 0, 0.1, 1, 1.5, 3, 32, 33.1, 128, NaN]) {
    for (const seed of [-0, -3.5, 7, NaN, Infinity, -Infinity, -2147483648, 2147483646, 2147483647, 9007199254740992]) {
        const result = kernel(count, seed);
        const lowered = descending(count, seed);
        console.log(result, result === 0 ? 1 / result : 0, lowered, lowered === 0 ? 1 / lowered : 0);
    }
}
const dynamic: any = kernel;
console.log(dynamic(0), dynamic(3), typeof dynamic(0));
function empty(count: number) { for (let i = 0; i < count; i++) {} return 8; }
console.log(empty(3));
let trace = 0;
function effect(value: number): number { trace++; return value + 1; }
function effects(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total = effect(total);
    return total;
}
console.log(effects(3, 7), trace);
function constant(): number { return 9; }
function constants(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total = constant();
    return total;
}
console.log(constants(3, 7));
