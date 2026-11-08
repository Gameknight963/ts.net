/// <reference lib="esnext.regexp" />
export {};
const input = 'ABCabcxyz012_ \n\u03B1\u03A3\u03C2\u212A\u017F\u3042\u30A2\u30FC\u{10400}\u{10428}\u{1F600}\uD800|\uDC00';
for (const flags of ['dgv', 'dgiv']) for (const pattern of [
    '[a-z]+', '[[a-z][0-9]]+', '[[a-z]&&[b-y]]+', '[[a-z]--[aeiou]]+',
    '[a&&a]', '[a--b]', '[a--a]', '[a&&a&&a]', '[a--b--c]',
    '[[a-z]&&[^aeiou]]+', '[[a-z]--[[b-d][x-z]]]+',
    '[\\p{Letter}&&\\p{ASCII}]+', '[\\p{scx=Hira}--\\p{sc=Hira}]+',
    '[\\p{Lowercase_Letter}--[a-z]]+', '\\P{Lowercase_Letter}+',
    '[^\\p{Lowercase_Letter}]+', '[\\P{Lowercase_Letter}]+',
    '[\\w--[a-z]]+', '[\\W]+', '[[\\d]--[02468]]+',
    '[]', '[^]', '[[]]', '[[^]]', '[\\u{10400}-\\u{1044F}]+',
    '[\\(\\)\\[\\]\\{\\}\\/\\-\\|]+', '[\\&\\!\\#\\%\\,\\:\\;\\<\\=\\>\\@\\~]+',
    '\\b', '\\B', '(?<letter>[a-z])\\k<letter>', '(.)\\1'
]) {
    // Node's iv ASCII-property shortcut omits Kelvin sign and long s.
    const sample = flags.includes('i') && pattern.includes('ASCII') ? input.replace(/[\u212A\u017F]/g, '') : input;
    const regex = new RegExp(pattern, flags);
    console.log('sets', flags, pattern, JSON.stringify([...sample.matchAll(regex)].map(match => [match[0], match.slice(1), match.index, match.groups, (match as any).indices])));
    console.log('replace', sample.replace(regex, '<$&:$1>'));
    console.log('split', JSON.stringify(sample.split(regex)));
}
for (const pattern of [
    '[a-b&&c]', '[ab&&c]', '[a&&bc]', '[a&&b--c]', '[a--b&&c]',
    '[a-]', '[-a]', '[a--]', '[&&a]', '[a&&&b]', '[a---b]', '[b-a]',
    '[[a]-b]', '[a-[b]]', '[\\d-a]', '[a-\\d]', '[a[b]',
    '[(]', '[)]', '[{]', '[}]', '[/]', '[|]', '[!!]', '[##]', '[$$]',
    '[%%]', '[**]', '[++]', '[,,]', '[..]', '[::]', '[;;]', '[<<]',
    '[==]', '[>>]', '[??]', '[@@]', '[^^]', '[~~]'
]) {
    try {new RegExp(pattern, 'v'); console.log('unexpected set syntax', pattern);}
    catch (error) {console.log('set syntax', pattern, (error as Error).name);}
}
const flags = new RegExp('a', 'ymvsigd');
console.log('flags', flags.flags, flags.unicode, flags.unicodeSets);
console.log('empty advance', JSON.stringify([...'a\u{1F600}b'.matchAll(/(?:)/gv)].map(match => match.index)));
const sticky = /./dvy;
sticky.lastIndex = 2;
const match = sticky.exec('a\u{1F600}b');
console.log('sticky', match?.[0], match?.index, sticky.lastIndex, JSON.stringify((match as any)?.indices));
console.log('literal subtraction', 'alphabet'.replace(/[[a-z]--[aeiou]]/gv, '_'));
for (const flags of ['u', 'v', 'u', 'v']) console.log('cache sets', flags, new RegExp('[a&&b]', flags).test('a'));
console.log('clone', structuredClone(/[[a-z]--[aeiou]]/gv).exec('ABCxyz')?.[0]);
const compiled: any = /x/;
compiled.compile('[[a-z]--[aeiou]]', 'giv');
console.log('recompile sets', compiled.flags, compiled.unicode, compiled.unicodeSets, JSON.stringify(compiled.exec('ABC')));
