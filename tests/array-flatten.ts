const nested: any[] = [1, [2, [3, [4]]], 5];
console.log('depth', nested.flat().join('|'), nested.flat(2).join('|'), nested.flat(Infinity).join('|'));
console.log('zero', nested.flat(0)[1] === nested[1], nested.flat(-1)[1] === nested[1], nested.flat(NaN)[1] === nested[1]);
console.log('fraction', nested.flat(1.9).join('|'), nested.flat(undefined).join('|'));
const sparse: any[] = []; sparse.length = 4; sparse[1] = [1, , 3]; sparse[3] = undefined;
console.log('holes', sparse.flat().join('|'), Object.keys(sparse.flat()).join(','), sparse.flat(0).length);
console.log('nullish', [null, undefined, [null, undefined]].flat().join('|'));
console.log('typed', [[1, 2], [3]].flat().reduce((sum, value) => sum + value, 0));
console.log('flatMap', [1, 2, 3].flatMap(value => [value, value * 2]).join(','));
console.log('one-level', [1].flatMap(value => [[value, value + 1]]).length, [1].flatMap(value => [[value]])[0][0]);
console.log('scalar-results', [1, 2].flatMap(value => value * 3).join(','));
console.log('empty-results', [1, 2, 3].flatMap(value => value === 2 ? [] : [value]).join(','));
const source = [1, 2, 3];
function mapper(this: unknown, value: number, index: number, array: number[]): number[] {
    console.log('arguments', arguments.length, this === undefined, array === source, index);
    return [value + index];
}
console.log('callback', source.flatMap(mapper).join(','));
const context = {offset: 10};
console.log('receiver', source.flatMap(function(this: typeof context, value) {return [value + this.offset];}, context).join(','));
const growing = [1, 2];
console.log('growth', growing.flatMap((value, index) => {if(index === 0) growing.push(3); return [value];}).join(','));
const shrinking = [1, 2, 3];
console.log('shrink', shrinking.flatMap((value, index) => {if(index === 0) shrinking.length = 1; return [value];}).join(','));
const flat: any = nested.flat, flatMap: any = nested.flatMap;
console.log('generic', flat.call({0: [1, 2], 2: 3, length: 3}).join(','), flat.call('ab').join(','));
const arrayLike = {0: 9, length: 1};
console.log('not-array', [arrayLike].flat()[0] === arrayLike, flatMap.call([1], () => arrayLike)[0] === arrayLike);
const inherited: any = {length: 2}; inherited.__proto__ = {1: [7, 8]};
console.log('inherited', flat.call(inherited).join(','));
const array: any[] = []; array.length = 2; (array as any).__proto__ = {0: [4]};
console.log('inherited-array', flat.call(array).join(','));
let effects = '';
const depth: any = {valueOf(): number {effects += 'D';return 1;}};
const getters = {get length(): number {effects += 'L';return 2;}, get 0(): number[] {effects += 'A';return [1];}, get 1(): number[] {effects += 'B';return [2];}};
console.log('order', flat.call(getters, depth).join(','), effects);
const cycle: any[] = []; cycle.push(cycle);
console.log('finite-cycle', cycle.flat(3).length, cycle.flat(3)[0] === cycle);
try {cycle.flat(Infinity);} catch(error) {console.log('infinite-cycle', error instanceof RangeError);}
let deep: any = 7; for(let i = 0; i < 1000; i++) deep = [deep];
console.log('deep', deep.flat(Infinity).join(','));
console.log('metadata', flat.name, flat.length, flatMap.name, flatMap.length);
try {flat.call(null);} catch(error) {console.log('invalid-receiver', error instanceof TypeError);}
try {flatMap.call([], undefined);} catch(error) {console.log('invalid-callback', error instanceof TypeError);}
try {[1].flatMap(() => {throw 'mapper failure';});} catch(error) {console.log('thrown', error);}
const depthMutation: any[] = [[1], [2]];
const mutatingDepth: any = {valueOf(): number {depthMutation.push([3]);depthMutation[0] = [9];return 1;}};
console.log('depth-mutation', depthMutation.flat(mutatingDepth).join(','));
let calls = 0;
console.log('sparse-mapper', sparse.flatMap(value => {calls++;return [value, , value];}).join('|'), calls);
const recursive: any[] = []; recursive.length = 1;
let reads = 0;
Object.setPrototypeOf(recursive, {get 0(): unknown {return ++reads < 4 ? recursive : 7;}});
console.log('changing-cycle', flat.call(recursive, Infinity).join(','), reads);
