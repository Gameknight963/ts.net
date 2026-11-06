/// <reference lib="es2023.array" />
const source = [1, 2, 3, 4];
console.log('basic', source.toSpliced(1, 2, 8, 9, 10).join(','), source.join(','));
console.log('insert', source.toSpliced(2, 0, 7).join(','), source.toSpliced(99, 0, 5).join(','));
console.log('remove', source.toSpliced(1).join(','), (source as any).toSpliced().join(','), source.toSpliced(0).length);
console.log('undefined', source.toSpliced(undefined as any).length, source.toSpliced(1, undefined).join(','));
for(const start of [-99, -4, -1, -1.9, 1.9, NaN, Infinity, -Infinity]) console.log('start', start, source.toSpliced(start, 1, 9).join(','));
for(const count of [-1, 0, 1.9, NaN, Infinity, -Infinity]) console.log('count', count, source.toSpliced(1, count).join(','));
const sparse: any[] = []; sparse.length = 4; sparse[2] = null;
const dense = sparse.toSpliced(1, 1);
console.log('dense', dense.join('|'), Object.keys(dense).join(','), Object.keys(sparse).join(','));
const object = {};
console.log('identity', ([object] as any).toSpliced()[0] === object, [].toSpliced(0, 0, object as never)[0] === object);
const splice: any = source.toSpliced;
console.log('generic', splice.call({0: 'a', 2: 'c', length: 3}, 1, 1, 'b').join(','), splice.call('abcd', 1, 2, 'x').join(''));
let effects = '';
const getters = {get length():number {effects += 'L';return 3;}, get 0():number {effects += 'A';return 1;}, get 1():number {effects += 'B';throw 'removed read';}, get 2():number {effects += 'C';return 3;}};
const start = {valueOf():number {effects += 'S';return 1;}};
const count = {valueOf():number {effects += 'D';return 1;}};
console.log('order', splice.call(getters, start, count, 7).join(','), effects);
const changing = [1, 2, 3];
console.log('mutation', splice.call(changing, {valueOf():number {changing.length = 1;return 1;}}, 0, 8).join('|'), changing.length);
const inherited: any[] = []; inherited.length = 3; Object.setPrototypeOf(inherited, {1: 'inherited'});
console.log('inherited', splice.call(inherited, 0, 0).join('|'));
console.log('empty', ([] as any).toSpliced().length, splice.call({length: -1}, 0, 0, 1).join(','));
console.log('large-delete', splice.call({length: 4294967296}, 0, Infinity, 7).join(','));
console.log('large-tail', splice.call({length: 4294967297, 4294967296: 'last'}, 0, 4294967296).join(','));
try {splice.call({length: 4294967296});} catch(error) {console.log('array-limit', error instanceof RangeError);}
try {splice.call({length: Infinity}, 0, 0, 1);} catch(error) {console.log('safe-limit', error instanceof TypeError);}
try {splice.call(null);} catch(error) {console.log('null', error instanceof TypeError);}
try {splice.call([1], {valueOf():number {throw 'start failure';}});} catch(error) {console.log('start-throw', error);}
try {splice.call([1], 0, {valueOf():number {throw 'count failure';}});} catch(error) {console.log('count-throw', error);}
try {splice.call({length: 1, get 0():number {throw 'read failure';}});} catch(error) {console.log('read-throw', error);}
console.log('metadata', splice.name, splice.length);
const readonlyValues: readonly string[] = ['a', 'b'];
console.log('readonly', readonlyValues.toSpliced(1, 0, 'c').join(','));
console.log('apply', splice.apply(source, [1, 2, 9]).join(','), splice.bind(source, 1, 1)(8).join(','));
let emptyEffects = '';
console.log('empty-conversions', splice.call({length: 0}, {valueOf():number {emptyEffects += 'S';return 0;}}, {valueOf():number {emptyEffects += 'D';return 0;}}).length, emptyEffects);
const mutatingCount = [1, 2, 3];
console.log('count-mutation', splice.call(mutatingCount, 1, {valueOf():number {mutatingCount[0] = 9;mutatingCount.push(4);return 1;}}).join(','));
const reversedSource = [1, 2, 3].toReversed();
console.log('chained', reversedSource.toSpliced(1, 1, 9).with(0, 7).join(','));
