function kernel(count: number, seed: number): number {
    let sum = 0;
    for (let i = 0; i < count; i++) {
        const [a, b] = [i + seed, i + 0.5];
        sum += a + b;
    }
    return sum;
}
console.log(kernel(5, 1));
function objectKernel(count: number, seed: number): number {
    let sum = 0;
    for (let i = 0; i < count; i++) {
        const {a, b} = {a: i + seed, b: i + 0.5};
        sum += a + b;
    }
    return sum;
}
console.log(objectKernel(5, 1));
function parameterKernel(count: number, seed: number): number {
    let sum = 0;
    for (let i = 0; i < count; i++) sum += parameterValue({a: i + seed, b: i + 0.5});
    return sum;
}
function parameterValue({a, b}: {a: number; b: number}) { return a + b; }
console.log(parameterKernel(5, 1));
