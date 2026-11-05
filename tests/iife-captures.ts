function kernel(count: number, seed: number): number {
    let value = seed;
    for (let i = 0; i < count; i++) value = (() => value + 1)();
    return value;
}
console.log(kernel(8, 3));
function ordered(seed: number) {
    let value = seed;
    return ((next: number) => value + next)(value = 7);
}
console.log(ordered(3));
function nested(seed: number) {
    return (() => ((delta: number) => seed + delta)(4))();
}
console.log(nested(3));
