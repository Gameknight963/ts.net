function work(): number {
    const fn = (value: number): number => value;
    Object.defineProperty(fn, "apply", {
        value: (target: {base: number}, values: number[]): number => target.base + values[0] + values.length - 1
    });
    const target = {base: 2};
    let total = 0;
    for (let index = 0; index < 100000; index++) total += fn.apply(target as any, [index]);
    return total;
}
console.log(work());
