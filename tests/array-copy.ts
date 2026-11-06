/// <reference lib="es2023.array" />
const numbers = [1, 2, 3];
console.log('reverse', numbers.toReversed().join(','), numbers.join(','));
console.log('with', numbers.with(1, 9).join(','), numbers.with(-1, 8).join(','), numbers.join(','));
console.log('index', numbers.with(NaN, 4).join(','), numbers.with(1.9, 4).join(','), numbers.with(-1.9, 4).join(','));
console.log('strings', ['a', 'b'].toReversed().join(','), ['a', 'b'].with(0, 'c').join(','));
const object = {value: 4};
const refs = [object, {value: 5}];
console.log('references', refs.toReversed()[1] === object, refs.with(1, object)[0] === object);
const sparse: any[] = []; sparse.length = 3; sparse[1] = null;
const reversed = sparse.toReversed(); const replaced = sparse.with(1, undefined);
console.log('dense', Object.keys(reversed).join(','), reversed.join('|'), Object.keys(replaced).join(','), sparse.length);
const reverse: any = numbers.toReversed, withValue: any = numbers.with;
console.log('generic', reverse.call({0: 'a', 2: 'c', length: 3}).join('|'), withValue.call('abc', 1, 'x').join(''));
console.log('empty', reverse.call({}).length, reverse.call({length: -1}).length, [].toReversed().length);
for(const index of [3, -4, Infinity, -Infinity]) {
    try {numbers.with(index, 9);} catch(error) {console.log('range', index, error instanceof RangeError);}
}
try {withValue.call([], 0, 9);} catch(error) {console.log('empty-range', error instanceof RangeError);}
try {reverse.call(null);} catch(error) {console.log('null', error instanceof TypeError);}
try {withValue.call(undefined);} catch(error) {console.log('undefined', error instanceof TypeError);}
let effects = '';
const getters = {get length():number {effects += 'L';return 3;}, get 0():number {effects += 'A';return 1;}, get 1():number {effects += 'B';throw 'should skip';}, get 2():number {effects += 'C';return 3;}};
console.log('skip-replaced', withValue.call(getters, {valueOf():number {effects += 'I';return 1;}}, 7).join(','), effects);
effects = '';
const reverseGetters = {length: 3, get 0():number {effects += 'A';return 1;}, get 1():number {effects += 'B';return 2;}, get 2():number {effects += 'C';return 3;}};
console.log('reverse-order', reverse.call(reverseGetters).join(','), effects);
const inherited: any[] = []; inherited.length = 3; Object.setPrototypeOf(inherited, {1: 'inherited'});
console.log('inherited', reverse.call(inherited).join('|'), withValue.call(inherited, 0, 'first').join('|'));
const mutation = [1, 2, 3];
console.log('conversion-mutation', withValue.call(mutation, {valueOf():number {mutation.length = 1;return -1;}}, 9).join('|'), mutation.length);
const changed = {length: 3, 0: 1, 1: 2, get 2():number {this[0] = 9;return 3;}};
console.log('getter-mutation', reverse.call(changed).join(','));
console.log('metadata', reverse.name, reverse.length, withValue.name, withValue.length);
console.log('missing', withValue.call([1, 2]).join('|'));
try {reverse.call({length: 4294967296, get 0():number {throw 'unexpected read';}});} catch(error) {console.log('length-limit', error instanceof RangeError);}
try {withValue.call({length: 4294967296}, 0, 1);} catch(error) {console.log('with-length-limit', error instanceof RangeError);}
const constructorRead = [1, 2] as any;
constructorRead.constructor = {get species(): unknown {throw 'constructor must not be read';}};
console.log('plain-copy', constructorRead.toReversed().join(','), constructorRead.with(0, 9).join(','));
console.log('ignored-arguments', reverse.call([1, 2], {valueOf():number {throw 'unused';}}).join(','));
effects = '';
try {withValue.call({length: 0}, {valueOf():number {effects += 'I';return 0;}}, 1);} catch(error) {console.log('empty-conversion', effects, error instanceof RangeError);}
try {withValue.call([1], {valueOf():number {throw 'index failure';}}, 2);} catch(error) {console.log('index-throw', error);}
try {reverse.call({length: 2, get 1():number {throw 'get failure';}});} catch(error) {console.log('get-throw', error);}
console.log('negative-zero', numbers.with(-0, 9).join(','));
const readonlyValues: readonly number[] = [1, 2, 3];
console.log('readonly', readonlyValues.toReversed().join(','), readonlyValues.with(0, 9).join(','));
