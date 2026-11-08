export {};
const headers = new Headers([['x-first', 'one']]);
Object.defineProperty(headers, 'label', {value: 'headers', enumerable: true});
Object.freeze(headers);
headers.append('x-second', 'two');
headers.set('x-first', 'changed');
console.log('headers', headers.get('x-first'), headers.get('x-second'), Object.isFrozen(headers), Object.keys(headers).join(','), Reflect.set(headers, 'label', 'other'));
const data = new FormData();
data.append('a', 'one');
Object.defineProperty(data, 'label', {value: 'form', enumerable: true});
Object.freeze(data);
data.append('b', 'two');
data.set('a', 'changed');
console.log('form', data.get('a'), data.get('b'), Object.isFrozen(data), Object.keys(data).join(','), Reflect.defineProperty(data, 'extra', {value: 1}));
for (const iterator of [headers.entries(), data.entries()]) {
    Object.defineProperty(iterator, 'label', {value: 'iterator', enumerable: true});
    Object.freeze(iterator);
    console.log('iterator', iterator.next().value!.join(':'), iterator.next().value!.join(':'), iterator.next().done, Object.isFrozen(iterator), Object.keys(iterator).join(','));
    const tag = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(iterator), Symbol.toStringTag)!;
    console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
}
headers.delete('x-first');
data.delete('a');
console.log('delete', headers.has('x-first'), data.has('a'));
for (const prototype of [Headers.prototype, FormData.prototype]) {
    const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
}
