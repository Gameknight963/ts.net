export {};
const input = [1, 2, 3, 4];
function missing(value: number): boolean {
    return value === 1 ? undefined as unknown as boolean : value > 2;
}
console.log('missing', input.filter(missing).join(','), input.every(missing), input.some(missing));
console.log('find', input.find(missing), input.findIndex(missing), input.findLast(missing), input.findLastIndex(missing));
function general(value: number): unknown { return value === 1 ? '' : value === 2 ? null : { value }; }
console.log('general', input.filter(general).join(','), input.every(general), input.some(general));
const source = [1, 2, 3];
const changed = source.filter((value, index, array) => { array[index] = value + 1; return array[index] > 2; });
console.log('source', changed.join(','), source.join(','));
let calls = 0;
function closure(limit: number): boolean {
    return input.every(value => { calls++; return value > limit; });
}
console.log('closure', closure(0), closure(2), calls);
let defaults = 0;
const optional = [1, undefined, 3].filter((value = ++defaults) => value > 0);
console.log('defaults', optional.join(','), defaults, Object.hasOwn(optional, '1'));
console.log('zero', input.filter(() => true).length, input.some(() => false));
function receiver(this: { limit: number }, value: number): boolean { return value > this.limit; }
console.log('receiver', input.filter(receiver, { limit: 2 }).join(','));
function argumentsCount(value: number): boolean { return arguments.length === 3 && value > 1; }
console.log('arguments', input.filter(argumentsCount).join(','));
let visited = 0;
try {
    input.filter(value => { visited++; if (value === 2) throw new Error('predicate'); return true; });
} catch (error) { console.log('throw', (error as Error).message, visited); }
