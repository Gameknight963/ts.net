export {};
const input = 'abcd abc ab a bc cd ABCD AB K\u212A S\u017F \u{1F600}a\u{1F600} \uD800x\uDC00';
for (const flags of ['dgv', 'dgiv']) for (const pattern of [
    '[\\q{a|ab|abc|abcd}]', '[\\q{abcd|abc|ab|a}]+',
    '[\\q{ab|bc|cd}x]', '[[\\q{ab|bc}]\\q{cd|x}]',
    '[\\q{ab|bc|cd}&&\\q{bc|cd|de}]',
    '[\\q{ab|bc|cd}--\\q{bc}]', '[[\\q{ab|bc}]--[a-z]]',
    '[[\\q{a|ab|bc}]&&[a-z]]', '[^[[\\q{ab|bc}]&&[a-z]]]',
    '[\\q{ab|a|}]', '[\\q{}]', '[\\q{|}]', '[\\q{ab|}]+',
    '[\\q{a|b}&&[a-z]]', '[^\\q{a|b}]',
    '[\\q{AB|ab}--\\q{ab}]', '[\\q{KS|ks}&&\\q{ks}]',
    '[\\q{\\u{1F600}a|\\u{1F600}}]', '[\\q{\\uD800x|x\\uDC00}]',
    '[\\q{\\u{D800}\\u{DC00}|\\uD800\\uDC00}]',
    '[\\q{\\(\\)|\\[\\]|\\{\\}|\\/\\-|\\|\\|}]',
    '([\\q{ab|a}])b', '(?<word>[\\q{ab|bc}])\\k<word>',
    '(?<=[\\q{abc|bc}])d', '(?=[\\q{ab|bc}])[a-z]+'
]) {
    // Node misses uppercase equivalents of one-character q alternatives here.
    const singleCharacterCase = pattern === '[[\\q{a|ab|bc}]&&[a-z]]' || pattern === '[\\q{a|b}&&[a-z]]' || pattern === '[^\\q{a|b}]' || pattern === '([\\q{ab|a}])b';
    const sample = flags.includes('i') && singleCharacterCase ? input.replace(/[A-Z]/g, character => character.toLowerCase()) : input;
    const regex = new RegExp(pattern, flags);
    console.log('strings', flags, pattern, JSON.stringify([...sample.matchAll(regex)].map(match => [match[0], match.slice(1), match.index, match.groups, (match as any).indices])));
    console.log('replace', sample.replace(regex, '<$&:$1:$<word>>'));
    console.log('callback', sample.replace(regex, (...args: any[]) => JSON.stringify(args)));
    console.log('split', JSON.stringify(sample.split(regex)));
}
for (const pattern of [
    '[^\\q{ab}]', '[^\\q{}]', '[^\\q{ab}--\\q{ab}]',
    '[^\\q{ab}&&\\q{cd}]', '[\\q{ab}-c]', '[a-\\q{b}]',
    '[\\q{ab]', '[\\q{ab|]', '[\\q]', '[\\q{(}]', '[\\q{a-b}]',
    '[\\q{\\d}]', '[\\q{\\p{Letter}}]', '\\q{ab}',
    '[\\q{a&&b}]', '[\\q{a/b}]'
]) {
    try {new RegExp(pattern, 'v'); console.log('unexpected string set', pattern);}
    catch (error) {console.log('string set syntax', pattern, (error as Error).name);}
}
for (const flags of ['gv', 'giv']) {
    const regex = new RegExp('[\\q{a|aa|aaa}]', flags);
    console.log('longest', flags, JSON.stringify('AAAAaaaa'.match(regex)));
    console.log('backtracking', new RegExp('^[\\q{abc|ab}]c$', flags).test('abc'));
}
