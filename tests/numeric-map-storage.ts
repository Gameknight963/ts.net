export {};
const source = [1, 2, 3];
const result: any = source.map((value, index, array): number => array[index] + value);
console.log('result', result.join(','), Array.isArray(result), Object.getPrototypeOf(result) === Array.prototype);
result[0] = 'text'; result.push({value: 7});
console.log('mixed', result[0], result[3].value, Object.keys(result).join(','));
Object.defineProperty(result, '1', {get() {return 19;}, configurable: true});
console.log('getter', result[1], result.map((value: any) => String(value)).join('|'));
delete result[2]; console.log('hole', 2 in result, result.length);
Object.freeze(result); try {result[0] = 11;} catch {console.log('frozen');}
const values = source.map((value): number => value === 1 ? -0 : value === 2 ? NaN : undefined as any);
console.log('numbers', Object.is(values[0], -0), Number.isNaN(values[1]), values[2], 2 in values);
const sparse: number[] = []; sparse.length = 4; sparse[2] = 5;
const mapped = sparse.map(value => value * 2);
console.log('sparse', mapped.length, Object.keys(mapped).join(','), mapped[2], 0 in mapped);
const captured: any[] = [];
class Output {constructor(length: number) {const value: any[] = new Array(length); captured.push(value); return value as any;}}
const custom: any = [1, 2]; custom.constructor = {[Symbol.species]: Output};
const output = custom.map((value: number) => value + 1);
console.log('species', output === captured[0], output.join(','));
const empty = source.slice(0, 0).map(value => value * 2);
console.log('empty', empty.length, empty.join(','));

const huge: number[] = []; huge.length = 1000000; huge[999999] = 9;
const hugeResult = huge.map(value => value + 1);
console.log('huge sparse', hugeResult.length, hugeResult[999999], Object.keys(hugeResult).length);

console.log('json numbers', JSON.stringify(values));
const converted: any = [1, 2].map(value => value + 1);
console.log('separator mutation', converted.join({toString() {converted[0] = 'changed'; return '|';}}));
const replaced = [1, 2].map(value => value + 1);
console.log('replacer mutation', JSON.stringify(replaced, function(key, value) {if (key === '0') replaced[1] = 13; return value;}));

const inherited = [1, 2, 3].map(value => value * 2);
const prototype = Object.create(Array.prototype);
Object.defineProperty(prototype, '0', {get() {throw new Error('own value must win');}});
Object.defineProperty(prototype, '1', {get() {return 17;}});
Object.setPrototypeOf(inherited, prototype);
console.log('own before prototype', inherited[0]);
delete inherited[1];
Object.defineProperty(inherited, '2', {value: 23, writable: false});
let sum = 0; for (const value of inherited) sum += value;
console.log('defined and inherited', inherited[0], inherited[1], inherited[2], sum);
