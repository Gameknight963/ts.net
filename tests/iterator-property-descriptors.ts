/// <reference lib="esnext.iterator" />
export {};
function drain(iterator: any): void {
    Object.defineProperty(iterator, 'label', {value: 'iterator', enumerable: true});
    Object.freeze(iterator);
    const values: string[] = [];
    while (true) {const step = iterator.next(); if (step.done) break; values.push(String(step.value));}
    console.log('values', values.join('|'), Object.isFrozen(iterator), Object.keys(iterator).join(','), Reflect.set(iterator, 'label', 'other'));
}
drain([1, 2].values());
drain(new Set([3, 4]).values());
drain(new Map([['value', 5]]).values());
drain(new Uint8Array([6, 7]).values());
drain('a\u{1f600}'[Symbol.iterator]());
drain('a1a'.matchAll(/a/g));
drain(Iterator.from([1, 2]).map(value => value + 1));
let position = 0;
drain(Iterator.from({next() {position++; return {value: position, done: position > 2};}}));
const concat = (Iterator as any).concat ?? function*(...inputs: any[]) {for (const input of inputs) yield* input;};
drain(concat([1], [2]));
function* generate() {yield 8; yield 9;}
drain(generate());
async function* asynchronous() {yield 10; await Promise.resolve(); yield 11;}
const asyncIterator = Object.freeze(asynchronous());
asyncIterator.next().then(first => asyncIterator.next().then(second => asyncIterator.next().then(last => {
    console.log('async', first.value, second.value, last.done, Object.isFrozen(asyncIterator));
})));
