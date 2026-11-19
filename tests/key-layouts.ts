type Pair = {a: number; b: number};
function make(first: boolean): Pair {
    if (first) return {b: 2, a: 1};
    return {b: 4, a: 3};
}
const first = make(true), second = make(false);
console.log(Object.keys(first)[0], Object.keys(second)[1], first.a, second.a, first === second);
const extra: any = first;
extra.z = 5;
console.log(Object.keys(first).length, Object.keys(second).length);

{
    const symbol = Symbol('layout');
    const reordered: {alpha: number, beta: number} = {beta: 2, alpha: 1};
    (reordered as any)[symbol] = 3;
    console.log('string-only reordered', Object.keys(reordered).join(','), Reflect.ownKeys(reordered).map(key => typeof key).join(','));
    const copied = {...reordered};
    delete (copied as any).beta;
    console.log('string-only spread', Object.keys(copied).join(','), Object.getOwnPropertySymbols(copied).length);
    const indexed: any = {alpha: 1, 2: 2};
    indexed[symbol] = 3;
    console.log('string-only indices', Object.keys(indexed).join(','));
    (reordered as any).gamma = 4;
    console.log('string-only extra string', Object.keys(reordered).join(','));
}
