const values = [1, 2, 1, 3, 1];
for (const start of [undefined, 0, 1, 3, 99, -1, -2, -99, 2.9, -2.9, NaN, Infinity, -Infinity])
    console.log('position', start, values.lastIndexOf(1, start));
console.log('omitted', values.lastIndexOf(1), values.lastIndexOf(7));
const sparse: any[] = []; sparse.length = 4; sparse[1] = undefined;
console.log('holes', sparse.lastIndexOf(undefined), sparse.lastIndexOf(undefined, 0));
console.log('equality', [NaN].lastIndexOf(NaN), [0].lastIndexOf(-0), [1 as any].lastIndexOf('1'));
const object = {};
console.log('identity', [object, {}].lastIndexOf(object), [{}, {}].lastIndexOf({}));
console.log('strings', ['a', 'b', 'a'].lastIndexOf('a'), [true, false, true].lastIndexOf(true));
const search: any = values.lastIndexOf;
console.log('generic', search.call({0: 'a', 2: 'a', length: 3}, 'a'), search.call('abca', 'a'));
const inherited = Object.setPrototypeOf({length: 4}, {2: 'found'});
console.log('inherited', search.call(inherited, 'found'));
let effects = '';
const source = {get length(): number {effects += 'L'; return 3;}, get 2(): string {effects += 'C'; return 'x';}, get 1(): string {effects += 'B'; return 'a';}, get 0(): string {effects += 'A'; return 'a';}};
const from = {valueOf(): number {effects += 'F'; return 2;}};
console.log('order', search.call(source, 'a', from), effects);
effects = '';
console.log('empty', search.call({length: 0}, 'a', from), effects);
const mutable = [1, 2, 1];
console.log('mutation', search.call(mutable, 1, {valueOf(): number {mutable.push(1);mutable[2] = 9;return 99;}}));
console.log('metadata', search.name, search.length);
try {search.call(null, 1);} catch (error) {console.log('null', error instanceof TypeError);}
try {search.call([1], 1, {valueOf(): number {throw 'conversion';}});} catch (error) {console.log('throw', error);}
const inheritedArray: any[] = []; inheritedArray.length = 3;
Object.setPrototypeOf(inheritedArray, {1: 'inherited'});
console.log('array-prototype', search.call(inheritedArray, 'inherited'));
console.log('large', search.call({length: 4294967298, 4294967297: 'large'}, 'large'));
console.log('clamp', search.call({length: Infinity, 9007199254740990: 'last'}, 'last'));
console.log('negative-zero', 1 / search.call([1], 1, -0));
console.log('missing-search', search.call([1, undefined]));
const extracted = values.lastIndexOf;
console.log('apply', extracted.apply(values, [1, 3]), extracted.bind(values, 1)(2));
const shrunk = [1, 2, 3];
console.log('shrink', search.call(shrunk, 3, {valueOf(): number {shrunk.length = 1;return 99;}}));
const changed: (number | undefined)[] = [1, 2, 3];
console.log('undefined-mutation', search.call(changed, undefined, {valueOf(): number {changed[2] = undefined;return 99;}}));
console.log('nan-packed', [1, NaN, 3].lastIndexOf(NaN));
