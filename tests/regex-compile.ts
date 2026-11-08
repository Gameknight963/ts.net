const regex = /a/g;
regex.lastIndex = 4;
console.log('identity', regex.compile('(?<word>b+)', 'di') === regex);
console.log('state', regex.source, regex.flags, regex.lastIndex);
const match = regex.exec('BB');
console.log('match', match?.[0], match?.groups?.word, match?.indices?.[0]?.join(':'));
const copy = /x/y;
copy.compile(regex as any);
console.log('copy', copy.source, copy.flags, copy.lastIndex, copy.test('BB'));
regex.lastIndex = 3;
for (const [pattern, flags] of [['[', 'g'], ['x', 'gg'], [/z/, 'i']] as const) {
    try { regex.compile(pattern as any, flags); }
    catch (error) { console.log('error', (error as Error).name, regex.source, regex.flags, regex.lastIndex); }
}
(regex.compile as any).call(regex);
console.log('empty', regex.source, regex.flags, regex.lastIndex, regex.test(''));
regex.compile(undefined as any, 'g');
console.log('undefined', regex.source, regex.flags);
regex.compile(/z/m as any, undefined);
console.log('regex', regex.source, regex.flags);
regex.compile(regex as any);
console.log('self', regex.source, regex.flags);
const compile = RegExp.prototype.compile;
for (const receiver of [{}, null, 1, RegExp.prototype]) {
    try { compile.call(receiver as any, 'x'); }
    catch (error) { console.log('receiver', (error as Error).name); }
}
class Derived extends RegExp { label = 'derived'; }
const derived = new Derived('a', 'g');
console.log('derived', derived.compile('b') === derived, derived.label, derived.test('b'));
const original = /a/g;
const pending = 'aba'.matchAll(original);
original.compile('b', 'g');
console.log('snapshot', Array.from(pending, value => value.index).join(','), original.exec('aba')?.index);
let order = '';
regex.compile({ toString() { order += 'p'; return 'c'; } } as any,
    { toString() { order += 'f'; return 'i'; } } as any);
console.log('coercion', order, regex.test('C'));
console.log('metadata', compile.name, compile.length);
