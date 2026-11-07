const params = new URLSearchParams('?a=1&a=2&empty&space=a+b&encoded=%2B%26%3D');
console.log(params.size, params.toString());
console.log(params.get('a'), params.getAll('a').join(','), params.get('missing') === null);
console.log(params.has('a'), params.has('a', '2'), params.has('a', '3'));
params.append('a', '3'); params.set('a', '4');
console.log(params.size, params.toString());
params.append('a', '5'); params.delete('a', '4');
console.log(params.getAll('a').join(','));
params.delete('a'); console.log(params.has('a'), params.toString());
const sorted = new URLSearchParams('b=1&a=first&b=2&a=second');
sorted.sort(); console.log(sorted.toString());
console.log([...sorted.keys()].join(','), [...sorted.values()].join(','));
console.log([...sorted].map(pair => pair.join(':')).join('|'));
console.log(sorted.entries === sorted[Symbol.iterator]);
const record = new URLSearchParams({a: '1', b: 'hello world'});
const tuples = new URLSearchParams([['a', '1'], ['a', '2']]);
const mapped = new URLSearchParams(Array.from(new Map([['a', '1'], ['b', '2']])));
console.log(record.toString(), tuples.toString(), mapped.toString());
const copied = new URLSearchParams(tuples); copied.append('a', '3');
console.log(tuples.toString(), copied.toString());
const special = new URLSearchParams();
special.append('~!*()\' /+', 'caf\u00e9\ud83d\ude00');
special.append('\ud800', '\udc00');
console.log(special.toString());
console.log(new URLSearchParams('bad=%FF%FE&broken=%C3%28&literal=%ZZ&old=%u0061').toString());
console.log(new URLSearchParams('&x&&=empty&equals=a=b&x=').toString());
const live = new URLSearchParams('a=1&b=2');
const iterator = live.entries();
console.log(JSON.stringify(iterator.next()));
live.append('c', '3'); live.set('b', 'changed');
console.log(JSON.stringify(iterator.next()), JSON.stringify(iterator.next()), JSON.stringify(iterator.next()));
live.append('d', '4'); console.log(JSON.stringify(iterator.next()));
const traversed: string[] = [];
const mutable = new URLSearchParams('a=1&b=2');
mutable.forEach((value, key, source) => { traversed.push(key + ':' + value); if (key === 'a') source.append('c', '3'); });
console.log(traversed.join(','));
const receiver = {prefix: 'value'};
record.forEach(function(this: typeof receiver, value, key, source) { console.log(this.prefix, key, value, source === record); }, receiver);
class Custom extends URLSearchParams { label = 'query'; first() { return this.get('a'); } }
const custom = new Custom('a=7');
console.log(custom.label, custom.first(), custom.size, custom instanceof Custom, custom instanceof URLSearchParams);
const Query = URLSearchParams; console.log(new Query('x=8').get('x'));
console.log(Object.prototype.toString.call(params), Object.prototype.toString.call(params.entries()));
for (const input of [[['a']], [['a', 'b', 'c']], ['ab']]) {
    try { new URLSearchParams(input as any); } catch (error) { console.log(error instanceof TypeError); }
}
console.log(new URLSearchParams({'\ud800': 'a', '\ud801': 'b'}).toString());

console.log(new URLSearchParams(special.toString()).get("~!*()' /+"));
console.log(new URLSearchParams('raw=caf\u00e9&nul=%00').toString());
