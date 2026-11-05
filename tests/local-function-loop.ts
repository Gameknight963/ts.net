function kernel(count: number, seed: number): number {
    let total = seed;
    for (let i = 0; i < count; i++) total = step(total);
    return total;
    function step(value: number): number { return value + 1; }
}
console.log(kernel(8, 3));
