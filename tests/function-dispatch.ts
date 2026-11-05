function apply(f: (n: number) => number, n: number): number { return f(n); }
function kernel(count: number, seed: number): number {
    const step = (n: number) => n + 1;
    let total = seed;
    for (let i = 0; i < count; i++) total = apply(step, total);
    return total;
}
console.log(kernel(8, 3));
let selected: (n: number) => number = n => n + 2;
function current(n: number): number { return apply(selected, n); }
console.log(current(3));
selected = n => n * 2;
console.log(current(3));
