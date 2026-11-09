/// <reference lib="es2023.array" />
const values = [1, 2, 3, 2];
console.log('found', values.find(value => value === 2), values.findIndex(value => value === 2), values.findLast(value => value === 2), values.findLastIndex(value => value === 2));
console.log('missing', values.find(value => value > 9), values.findIndex(value => value > 9), values.findLast(value => value > 9), values.findLastIndex(value => value > 9));
const sparse: (number | undefined)[] = [1]; sparse.length = 4; sparse[3] = 7;
let visits = '';
console.log('hole', sparse.findIndex((value, index) => {visits += index + ':' + value + ';';return value === undefined;}), visits);
visits = '';
console.log('hole-right', sparse.findLastIndex((value, index) => {visits += index + ':' + value + ';';return value === undefined;}), visits);
const shrinking: (number | undefined)[] = [1, 2, 3];
console.log('shrink', shrinking.findIndex((value, index) => {if(index === 0) shrinking.length = 1;return value === undefined;}));
const growing = [1, 2];
visits = '';
console.log('grow', growing.find((value, index) => {visits += index;growing.push(9);return value === 9;}), visits);
const changing = [{value: 1}];
const original = changing[0];
console.log('original', changing.find((value, index, source) => {source[index] = {value: 2};return true;}) === original);
const find: any = values.find, findIndex: any = values.findIndex;
const findLast: any = values.findLast, findLastIndex: any = values.findLastIndex;
const object = {1: 'middle', length: 3};
console.log('generic-holes', findIndex.call(object, (value: unknown) => value === undefined), findLastIndex.call(object, (value: unknown) => value === undefined));
console.log('generic-value', find.call(object, (value: unknown) => value !== undefined), findLast.call(object, (value: unknown) => value !== undefined));
const inherited: any = {length: 2}; inherited.__proto__ = {0: 'inherited'};
console.log('inherited', find.call(inherited, () => true), findLastIndex.call(inherited, (value: unknown) => value !== undefined));
const array: any[] = []; array.length = 2; (array as any).__proto__ = {1: 'prototype'};
console.log('array-inherited', findLast.call(array, () => true));
console.log('string', find.call('abc', (value: string, index: number, source: any) => {if(index === 0) console.log('boxed', typeof source, source.valueOf());return value === 'b';}));
const context = {target: 3};
console.log('receiver', values.find(function(this: typeof context, value) {return value === this.target;}, context));
function predicate(this: unknown, value: number, index: number, source: number[]): boolean {
    console.log('arguments', arguments.length, this === undefined, source === values, index);
    return value === 1;
}
console.log('callback', values.find(predicate));
let effects = '';
const accessors = {get length(): number {effects += 'L';return 3;}, get 0(): number {effects += 'A';return 1;}, get 2(): number {effects += 'C';return 3;}};
console.log('order', findLast.call(accessors, (value: number) => {effects += 'P';return value === 3;}), effects);
for (const method of [find, findIndex, findLast, findLastIndex]) {
    console.log('metadata', method.name, method.length, method.call([], () => true));
    try {method.call([], undefined);} catch(error) {console.log('invalid-callback', error instanceof TypeError);}
    try {method.call(null, () => true);} catch(error) {console.log('invalid-receiver', error instanceof TypeError);}
}
try {values.find(() => {throw 'predicate failure';});} catch(error) {console.log('thrown', error);}
const mixed: (string | number)[] = ['one', 2, 'three', 4];
console.log('type-guard', mixed.find((value): value is number => typeof value === 'number'), mixed.findLast((value): value is number => typeof value === 'number'));
console.log('numeric backwards', [0,2,NaN,3,0].findLast(value=>value), [0,2,NaN,3,0].findLastIndex(value=>value));
const reverseMutation=[1,2,3];
console.log('numeric backwards mutation', reverseMutation.findLast((value,index)=>{
 reverseMutation[index]=99;return value;
}),reverseMutation.join(','));
