function sumSquares(count: number): number {
    let total = 0;
    for (let i = 0; i < count; i++) {
        const value = i % 1000;
        total += value * value;
    }
    return total;
}

console.log(sumSquares(10000000));
