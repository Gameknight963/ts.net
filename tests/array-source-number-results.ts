const values = [0, -0, NaN, 2, 3];
function predicate(value: number, index: number, source: number[]): number | undefined {
    return index === 4 ? undefined : source[index];
}
console.log('filter', values.filter(predicate).join(','));
console.log('every some', values.every(predicate), values.some(predicate));
console.log('find', values.find(predicate), values.findIndex(predicate));
console.log('last', (values as any).findLast(predicate), (values as any).findLastIndex(predicate));
const mapped = values.map(predicate);
console.log('map', mapped.join(','), Object.is(mapped[1], -0), Number.isNaN(mapped[2]), mapped[4] === undefined);
let visits = 0;
console.log('forEach', values.forEach((value, index, source) => {visits++; return source[index];}), visits);
const sparse = [1, 2, 3];
delete sparse[1];
console.log('sparse', sparse.filter((value, index, source) => source[index]).join(','));
const inherited = Object.create(Array.prototype);
Object.defineProperty(inherited, '1', {get() {console.log('inherited'); return 5;}, configurable: true});
Object.setPrototypeOf(sparse, inherited);
console.log('prototype', sparse.filter((value, index, source) => source[index]).join(','));
const changing = [1, 2, 3];
let reads = 0;
console.log('accessor', changing.filter((value, index, source) => {
    if (index === 0) Object.defineProperty(source, '1', {get() {reads++; return 0;}, configurable: true});
    return source[index];
}).join(','), reads);
const shrinking = [1, 2, 3];
console.log('shrinking', shrinking.find((value, index, source) => {
    if (index === 0) source.length = 1;
    console.log('visit', index, value);
    return index === 0 ? 0 : source[index];
}));
const generic = {0: 0, 1: 7, 2: NaN, length: 3};
console.log('generic', Array.prototype.filter.call(generic, (value: number, index: number, source: any): number => source[index]).join(','));
function argumentsPredicate(value: number, index: number, source: number[]): number {
    console.log('arguments', arguments.length, arguments[2] === source);
    return source[index];
}
console.log('arguments result', [1, 2].filter(argumentsPredicate).join(','));

console.log('generic numeric predicate', Array.prototype.filter.call(generic, (value: number, index: number): number => value + index).join(','));
let genericTotal = 0;
Array.prototype.forEach.call(generic, (value: number, index: number): number => {genericTotal += index; return value;});
console.log('generic numeric forEach', genericTotal);
