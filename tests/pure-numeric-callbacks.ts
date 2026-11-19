export {};
function twice(value: number): number { return value * 2; }
const input = [0, 1, 2, 3, 4];
console.log(input.map(value => twice(value) + 1).filter(value => value > 3).join(','));
console.log(input.map((value, index) => value + index).join(','));
console.log(input.every(value => value < 5), input.some(value => value === 3), input.findIndex(value => value > 2));
const special: number[] = [-0, NaN, Infinity, -Infinity, undefined as any];
const identity = special.map(value => value);
console.log(identity.join(','), Object.is(identity[0], -0), identity[4] === undefined);
console.log(special.filter(value => value === value).join(','));

// Numeric storage must still be checked when the callback can mutate it.
const changed = [1, 2, 3];
console.log(changed.map(value => { changed[1] = 20; return value * 2; }).join(','));
const shrunk = [1, 2, 3];
console.log(shrunk.filter(value => { shrunk.length = 1; return value > 0; }).join(','));
const materialized = [1, 2, 3].map(value => value * 2);
Object.defineProperty(materialized, '1', {get() {materialized[2] = 30; return 20;}, configurable: true});
console.log(materialized.map(value => value + 1).join(','));
let offset = 1;
console.log(input.map(value => value + offset).join(','));
offset = 10;
console.log(input.map(value => value + offset).join(','));

const source = [1, 2, 3];
class Custom extends Array<number> {
    constructor(length: number) { super(length); source[1] = 40; }
}
(source as any).constructor = {[Symbol.species]: Custom};
const result = source.map(value => value * 2);
console.log(result instanceof Custom, result.join(','));
const sparse = [1, , 3].map(value => value as number);
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '1', {get() {return 20;}, configurable: true});
Object.setPrototypeOf(sparse, prototype);
console.log(sparse.map(value => value * 2).join(','));

const helperSource = [1, 2, 3];
function mutate(value: number): number { helperSource[1] = 99; return value * 2; }
console.log(helperSource.map(value => mutate(value)).join(','));
let replaceable = (value: number): number => value * 2;
console.log(input.map(value => replaceable(value)).join(','));
replaceable = value => { input[1] = 50; return value * 3; };
console.log(input.map(value => replaceable(value)).join(','));
