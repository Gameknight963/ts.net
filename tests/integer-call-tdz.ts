function step(value: number): number { return value + 1; }
function run(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total = alias(total);
    return total;
}
console.log(run(128, 7));
const alias = step;
