export {};

function read(items: readonly any[], index: number): any {return items[index];}
const values: any[] = [1, 'two', , 4];
const named: any = values;
named[-1] = 'negative';
named[0.5] = 'fractional';
named[NaN] = 'nan';
named[Infinity] = 'infinity';
named[-Infinity] = 'negative infinity';
named[4294967294] = 'last index';
named[4294967295] = 'ordinary property';
named[4294967296] = 'larger property';
for (const index of [0, -0, 1, 2, -1, 0.5, NaN, Infinity, -Infinity, 4294967294, 4294967295, 4294967296])
    console.log('helper index', index, read(values, index));

let getterCalls = 0;
Object.defineProperty(values, '1', {get() {getterCalls++; return 'getter';}, configurable: true});
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '2', {get() {getterCalls++; return this === values ? 'inherited' : 'wrong receiver';}});
Object.setPrototypeOf(values, prototype);
console.log('helper accessors', read(values, 1), read(values, 2), getterCalls);

const numbers = [3, 4];
console.log('number array helper', read(numbers, 0), read(numbers, 1), read(numbers, 2));
const tagged: (number | string)[] = [5, 'tagged'];
console.log('tagged helper', typeof tagged[0] === 'number', read(tagged, 0), read(tagged, 1));
const reversed: any[] = ['left', 'right'];
reversed.reverse();
console.log('reversed helper', read(reversed, 0), read(reversed, 1));
function template(parts: TemplateStringsArray): void {
    console.log('template helper', JSON.stringify(read(parts, 0)), JSON.stringify(read(parts.raw, 0)));
}
template`first\nsecond`;
