/// <reference lib="es2023.array" />
const values = [10, 2, 30, 1];
console.log('default', values.toSorted().join(','), values.join(','));
console.log('numeric', values.toSorted((a, b) => a - b).join(','), values.toSorted((a, b) => b - a).join(','));
console.log('strings', ['z', 'a', 'B', 'aa', '\uD800', '\uD83D\uDE00'].toSorted().map(s => s.charCodeAt(0)).join(','));
const objects = [{key: 2, id: 'a'}, {key: 1, id: 'b'}, {key: 2, id: 'c'}, {key: 1, id: 'd'}];
console.log('stable', objects.toSorted((a, b) => a.key - b.key).map(o => o.id).join(','));
console.log('nan-equal', objects.toSorted(() => NaN).map(o => o.id).join(','));
console.log('zero-equal', objects.toSorted(() => -0).map(o => o.id).join(','));
const sparse: any[] = []; sparse.length = 5; sparse[1] = 3; sparse[3] = undefined; sparse[4] = 1;
let sawUndefined = false;
const dense = sparse.toSorted((a, b) => {if (a === undefined || b === undefined) sawUndefined = true;return a - b;});
console.log('holes', dense.join('|'), Object.keys(dense).join(','), Object.keys(sparse).join(','), sawUndefined);
console.log('nullish', [undefined, null, 'undefined', 'null'].toSorted().join('|'));
const sort: any = values.toSorted;
console.log('generic', sort.call({0: 'z', 2: 'a', length: 3}).join('|'), sort.call('cab').join(''));
const inherited: any[] = []; inherited.length = 3; Object.setPrototypeOf(inherited, {1: 4});
console.log('inherited', sort.call(inherited).join('|'));
let effects = '';
const getters = {get length():number {effects += 'L';return 3;}, get 0():number {effects += 'A';return 3;}, get 1():number {effects += 'B';return 2;}, get 2():number {effects += 'C';return 1;}};
let firstComparison = true;
console.log('read-order', sort.call(getters, (a:number, b:number) => {if(firstComparison) {console.log('before-comparison', effects); firstComparison = false;}return a-b;}).join(','));
let callbackValid = true;
function compare(this: unknown, a:number, b:number):number {callbackValid = callbackValid && this === undefined && arguments.length === 2;return a - b;}
console.log('callback', values.toSorted(compare).join(','), callbackValid);
console.log('converted', sort.call([3, 1, 2], (a:number, b:number) => ({valueOf():number {return a - b;}})).join(','));
const mutation = [3, 2, 1];
console.log('snapshot', mutation.toSorted((a, b) => {mutation.length = 0;return a - b;}).join(','), mutation.length);
const identity = {};
console.log('identity', [identity].toSorted()[0] === identity, [].toSorted().length, [1].toSorted()[0]);
try {sort.call({get length():number {throw 'length read';}}, 7);} catch(error) {console.log('validation-first', error instanceof TypeError);}
try {sort.call(null);} catch(error) {console.log('null', error instanceof TypeError);}
try {sort.call([1, 2], () => {throw 'callback failure';});} catch(error) {console.log('callback-throw', error);}
try {sort.call([1, 2], () => ({valueOf():number {throw 'conversion failure';}}));} catch(error) {console.log('conversion-throw', error);}
try {sort.call({length: 1, get 0():number {throw 'element failure';}});} catch(error) {console.log('read-throw', error);}
try {sort.call({length: 4294967296});} catch(error) {console.log('array-limit', error instanceof RangeError);}
console.log('metadata', sort.name, sort.length);
const readonlyValues: readonly number[] = [3, 2, 1];
console.log('readonly', readonlyValues.toSorted().join(','), sort.apply(values, [(a:number,b:number)=>a-b]).join(','));
const textObjects = [{toString():string {return 'b';}}, {toString():string {return 'a';}}];
console.log('object-text', textObjects.toSorted()[0] === textObjects[1]);
console.log('undefined-comparator', values.toSorted(undefined).join(','));
console.log('converted-undefined', sort.call([3, 2, 1], () => undefined).join(','));
const stableMany: {key:number, id:number}[] = [];
for(let i = 0; i < 1000; i++) stableMany.push({key: i & 7, id: i});
const sortedMany = stableMany.toSorted((a,b) => a.key - b.key);
let stable = true;
for(let i = 1; i < sortedMany.length; i++) {
    const previous = sortedMany[i - 1], current = sortedMany[i];
    if (previous.key > current.key || previous.key === current.key && previous.id > current.id) stable = false;
}
console.log('stable-large', stable, sortedMany[0].id, sortedMany[999].id);
