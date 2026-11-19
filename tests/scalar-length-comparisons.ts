export {};
function compare(value: number, length: any): boolean[] {
    const view = { length };
    const alias = view;
    return [value < view.length, value <= alias.length, value > view.length, value >= alias.length];
}
for (const length of [-1, 0, 0.5, 2.5, '2.5', null, undefined, NaN, true, false])
    console.log('compare', String(length), compare(1, length).join(','));
let conversions = 0;
const coercible = { valueOf() { conversions++; return 2; } };
console.log('coercion', compare(1, coercible).join(','), conversions);
try { compare(1, Symbol('length')); } catch { console.log('symbol throws'); }
function changed(length: any): boolean {
    const view = { length: undefined as any };
    view.length = length;
    return 1 < view.length;
}
console.log('write', changed(2), changed(0), changed(undefined));
function count(length: any): number {
    const view = { length };
    let total = 0;
    for (let i = 0; i < view.length; i++) total++;
    return total;
}
console.log('loop', count(2.5), count(undefined), count(-1));
