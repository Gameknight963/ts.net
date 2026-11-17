export {};
function arithmetic(): void {
    let value = 10n;
    const before = value;
    const assigned = value += 3n;
    value -= 2n; value *= 7n; value /= 3n; value %= 19n;
    value &= 15n; value |= 32n; value ^= 7n;
    console.log('arithmetic', before, assigned, value, -value, ~value, value + (2n * 3n));
    value = -123456789012345678901234567890n;
    console.log('large', value / 7n, value % 7n, value);
}
arithmetic(); arithmetic();
function snapshots(): void {
    let value = 1n;
    const values: bigint[] = [];
    for (let i = 0; i < 4; i++) { values.push(value); value += 1n; }
    console.log('snapshots', values.join(','), value);
}
snapshots();
function zero(): void {
    let value = 7n;
    try { value /= 0n; } catch (error) { console.log('divide zero', (error as Error).name, value); }
    try { value %= 0n; } catch (error) { console.log('remainder zero', (error as Error).name, value); }
}
zero();
function changing(): void {
    let value: any = 1n;
    value = 2; console.log('number', typeof value, value);
    value = 'text'; console.log('text', typeof value, value);
    value = undefined; console.log('undefined', value === undefined);
}
changing();
function dependent(): void {
    let first: any = 1n, second: any = 2n;
    second = 'text'; first = second + 3n;
    console.log('dependent', first, second);
}
dependent();
function linked(flag: boolean): void {
    let first = 1n, second = 2n;
    first = second + 1n; second = first * 2n;
    const snapshot = flag ? (first += 2n) : (first -= 1n);
    first = 9n;
    console.log('linked', snapshot, first, second);
}
linked(false); linked(true);
function captured(): void {
    let value: any = 1n;
    const write = (): void => { value = 2; };
    write(); console.log('captured', typeof value, value);
}
captured();
function patterns(): void {
    let value: any = 1n;
    [value] = [2]; console.log('array pattern', typeof value, value);
    ({ value } = { value: 'text' }); console.log('object pattern', typeof value, value);
}
patterns();
function forward(): void {
    console.log('forward', value);
    var value: bigint | undefined = 3n;
    console.log('initialized', value);
}
forward();
function conditional(flag: boolean): void {
    if (flag) { var value: bigint | undefined = 4n; }
    console.log('conditional', value);
}
conditional(false); conditional(true);
function iterations(): void {
    let value: any = 1n;
    console.log('update', value++, ++value, value);
    for (value of [2, 'text']) console.log('iteration', typeof value, value);
}
iterations();
function mixed(): void {
    let value: any = 1n;
    try { value += 2; } catch (error) { console.log('mixed', (error as Error).name, value); }
}
mixed();
function conversion(): void {
    let value = 1n, calls = 0;
    value = BigInt({ toString(): string { calls++; return '5'; } } as any);
    console.log('conversion', value, calls);
}
conversion();
export function work(): number {
    let value = 0n;
    for (let i = 0; i < 100000; i++) { value += 3n; value -= 2n; }
    return Number(value);
}
console.log('loop', work());
