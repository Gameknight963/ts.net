export {};
function kernel(count: number, seed: number): number {
    const source = {x: seed, y: 2};
    const copies: {x: number; y: number}[] = [];
    for (let i = 0; i < count; i++) {
        source.x = i + seed;
        const copy = {...source};
        copy.y = i;
        copies.push(copy);
    }
    let total = 0;
    for (const copy of copies) total += copy.x + copy.y;
    return total;
}

console.log(kernel(0,7),kernel(1,7),kernel(10,7),kernel(1000,7));
console.log('bounds',kernel(-1,7),kernel(NaN,7),kernel(0.5,7),kernel(1.5,7));
