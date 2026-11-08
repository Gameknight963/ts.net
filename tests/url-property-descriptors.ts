export {};
const url = new URL('https://example.com/?a=1');
const params = url.searchParams;
Object.defineProperty(url, 'label', {value: 'address', enumerable: true});
Object.freeze(url);
Object.freeze(params);
url.pathname = '/next';
params.append('b', '2');
console.log('url', url.href, url.searchParams === params, Object.isFrozen(url), Object.keys(url).join(','), Reflect.set(url, 'label', 'changed'));
url.search = '?c=3';
console.log('linked', params.toString(), url.href, Object.isFrozen(params));
params.set('c', '4');
console.log('params', url.search, Reflect.defineProperty(params, 'extra', {value: 1}));
const iterator = params.entries();
Object.defineProperty(iterator, 'label', {value: 'entries', enumerable: true});
Object.freeze(iterator);
console.log('iterator', iterator.next().value!.join(':'), iterator.next().done, Object.isFrozen(iterator), Object.keys(iterator).join(','));
const custom = new URLSearchParams('x=1');
Object.defineProperty(custom, 'size', {value: 42, configurable: true});
Object.seal(custom);
custom.append('y', '2');
console.log('shadow', custom.size, custom.toString(), Object.isSealed(custom), Reflect.deleteProperty(custom, 'size'));
for (const prototype of [URL.prototype, URLSearchParams.prototype, Object.getPrototypeOf(iterator)]) {
    const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
}
