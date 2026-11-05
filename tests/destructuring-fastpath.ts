function kernel(count: number, seed: number): number {
    let sum = 0;
    for (let i = 0; i < count; i++) {
        const [a, b] = [i + seed, i + 0.5];
        sum += a + b;
    }
    return sum;
}
console.log(kernel(5, 1));
