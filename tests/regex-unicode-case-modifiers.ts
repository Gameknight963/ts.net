export {};
const patterns = [
    '(?i:ab)c', '(?-i:ab)c', '(?i:a(?-i:b)c)d', '(?i:(?-i:a)b)',
    '(?i:a)|b', '(?i:[A-Z]+)', '(?i:\\p{Lowercase_Letter}+)',
    '(?-i:\\p{Lowercase_Letter}+)', '(?im-s:^a.b$)', '(?s-i:a.b)',
    '(?i:(a))\\1', '(?-i:(a)\\1)', '(?i:(s)\\1)', '(?i:(s))\\1',
    '(?i:(?<x>s)\\k<x>)', '(?i:\\b(s)\\1\\b)',
    '(?i:(s)\\1)(?-i:[Ss\\u017F])', '(?i:(s)\\1)(?-i:\\d)',
    '(?i:(s)\\1)(?-i:.)', '(?i:(s)\\1)(?-i:\\p{Regional_Indicator})',
    '(?i:(s)\\1)(?-i:[^A-Za-z\\u017F\\u212A])',
    '(?<=(?i:a))b', '(?<=(?i:(s)\\1))\\d', '(?i:(?=(s)\\1)\\w+)',
    '(?i:(?<A>a))(?-i:\\k<A>)',
    '(?i:(?<x>s)|(?<x>k))(?-i:\\k<x>)'
];
for (const flags of ['dgu', 'dgiu', 'dgv', 'dgiv']) for (const pattern of patterns) {
    for (const text of [
        'abc', 'ABC', 'ABc', 'aBcD', 'AbCd', 'aa AA Aa aA',
        'ss SS Ss sS', '\u017FS \u017F\u017F s\u017F', '\u212A\u212A \u212Ak kK',
        'ss1 \u017FS1 SS1', 'ss\u{1F600} ss\u{1F1FA}', 'a\nb\nA\nB', 'ab AB aB', '\u{10400}\u{10428}'
    ]) {
        const regex = new RegExp(pattern, flags);
        console.log('case modes', flags, pattern, text, JSON.stringify([...text.matchAll(regex)].map(match => [match[0], match.slice(1), match.index, match.groups, (match as any).indices])));
    }
    const regex = new RegExp(pattern, flags);
    const text = 'aa AA Aa ss SS \u017FS1 ABC abc';
    console.log('replacement', flags, pattern, text.replace(regex, '<$&:$1:$<x>>'));
    console.log('callback', flags, pattern, text.replace(regex, (...args: any[]) => JSON.stringify(args)));
    console.log('split', flags, pattern, JSON.stringify(text.split(regex)));
    console.log('test', flags, pattern, regex.test(text), regex.lastIndex);
}
for (const pattern of ['(?i:[\\q{ab|cd}])(?-i:x)', '(?i:(\\p{RGI_Emoji})\\1)', '(?-i:(\\p{RGI_Emoji})\\1)']) {
    for (const flags of ['dgv', 'dgiv']) {
        const regex = new RegExp(pattern, flags);
        const text = 'ABx CDx abX \u{1F600}\u{1F600}';
        console.log('string case modes', flags, pattern, JSON.stringify([...text.matchAll(regex)].map(match => [match[0], match.slice(1), (match as any).indices])));
    }
}
console.log('literal case mode', /(?i:ab)c/u.test('ABc'), /(?-i:ab)c/iu.test('abC'));
for (const flags of ['u', 'iu', 'v', 'iv']) {
    const regex = new RegExp('(?i:(s)\\1)', flags);
    const cloned = structuredClone(regex);
    console.log('clone case mode', flags, cloned.flags, JSON.stringify(cloned.exec('\u017FS')));
}
const compiled: any = /x/;
compiled.compile('(?i:(s)\\1)', 'u');
console.log('compile scoped fold', JSON.stringify(compiled.exec('\u017FS')));
compiled.compile('(?-i:(s)\\1)', 'iu');
console.log('compile scoped exact', compiled.exec('\u017FS'), JSON.stringify(compiled.exec('ss')));
