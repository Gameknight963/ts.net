function kernel(count: number, seed: number): number {
    let total = 0;
    let value = seed;
    for (let i = 0; i < count; i++) {
        value += 1;
        if (value > 1000) { value = 1; }
        total += value * value;
    }
    return total;
}

let checksum = 0;
for (let warmup = 0; warmup < 10; warmup++) {
    checksum += kernel(1000000, warmup);
}
for (let trial = 0; trial < 5; trial++) {
    const start = performance.now();
    checksum += kernel(20000000, trial);
    console.log(performance.now() - start);
}
console.log("checksum", checksum);
