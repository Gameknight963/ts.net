export {};
const patterns = [
    '(?<x>a)|(?<x>b)', '^(?:(?<x>a)|(?<x>b))$',
    '(?:(?<x>a)|(?<x>b))+', '(?:(?<x>a)|(?<x>b))+\\k<x>',
    '(?:(?<x>a)|(?<x>b))+\\1\\2',
    '(?<x>a)\\1|(?<x>b)\\2', '\\k<x>(?:(?<x>a)|(?<x>b))',
    '(?:(?<x>a)|(?:(?<x>b)|(?<x>c)))',
    '(?:(?<x>a)(?<y>b)|(?<y>c)(?<x>d))',
    '(?<=(?<x>a)|(?<x>b))c', '(?<=(?:(?<x>a)|(?<x>b))+)c',
    '(?=(?<x>a))a|(?=(?<x>b))b', '(?<x>a)|(?<\\u0078>b)',
    '(?:(?<$x>a)|(?<$x>b))+', '(?:(?<x>a)|(?<x>b)){1,3}',
    '(?:(?<x>a)|(?<x>b))+?c', '(?:(?<x>a)|(?<x>b))+a'
];
for (const flags of ['dg', 'dgu', 'dgv', 'dgiu', 'dgiv']) {
    for (const pattern of patterns) {
        for (const text of ['a', 'b', 'c', 'ab', 'ba', 'aba', 'bab', 'aa', 'bb', 'abb', 'baa', 'abcd', 'abac', 'bac', 'ABa']) {
            const regex = new RegExp(pattern, flags);
            const matches = [...text.matchAll(regex)].map(match => {
                const indices: any = (match as any).indices;
                const aliases = Object.keys(match.groups!).map(name => [name, indices.groups[name] === undefined || indices.some((entry: any, index: number) => index > 0 && entry === indices.groups[name])]);
                return [match[0], match.slice(1), match.index, match.groups, indices, aliases];
            });
            console.log('duplicate', flags, pattern, text, JSON.stringify(matches));
        }
        const text = 'ab aba bb bac cd ABa';
        const regex = new RegExp(pattern, flags);
        console.log('replacement', flags, pattern, text.replace(regex, '<$&:$1:$2:$<x>:$<y>>'));
        console.log('callback', flags, pattern, text.replace(regex, (...args: any[]) => JSON.stringify(args)));
        console.log('split', flags, pattern, JSON.stringify(text.split(regex)));
        console.log('test', flags, pattern, regex.test(text), regex.lastIndex);
    }
}
for (const pattern of [
    '(?<x>a)(?<x>b)', '(?<x>(?<x>a)|b)', '(?<x>a)(?:(?<x>b)|c)',
    '(?<x>a)|(?<x>b)(?<x>c)', '(?:(?<x>a)|b)(?:(?<x>c)|d)',
    '(?=(?<x>a))(?<x>a)', '(?<x>a)(?<\\u0078>b)'
]) {
    try {new RegExp(pattern); console.log('unexpected duplicate syntax', pattern);}
    catch (error) {console.log('duplicate syntax', pattern, (error as Error).name);}
}
const cloned = structuredClone(new RegExp('(?<x>a)|(?<x>b)', 'dv'));
console.log('clone duplicate', JSON.stringify(cloned.exec('b')?.groups));
const compiled: any = /(?<before>x)/;
compiled.compile('(?<x>a)|(?<x>b)', 'd');
console.log('compile duplicate', JSON.stringify(compiled.exec('b').groups));
compiled.compile('(?<after>c)', 'd');
console.log('compile unique', JSON.stringify(compiled.exec('c').groups));
const alternatives = 'abcdefghijkl'.split('').map(letter => '(?<x>' + letter + ')').join('|');
for (const flags of ['d', 'du', 'dv', 'div']) {
    const many = new RegExp('(?:' + alternatives + ')\\12', flags);
    for (const text of ['a', 'hh', 'll', 'LL']) {
        const match = many.exec(text);
        console.log('many duplicates', flags, text, JSON.stringify(match), JSON.stringify(match?.groups), JSON.stringify((match as any)?.indices));
    }
    const collision = new RegExp('(?:(?<x>a)(?<xOccurrence2>b)|(?<x>c))', flags);
    console.log('occurrence collision', flags, JSON.stringify(collision.exec('ab')?.groups), JSON.stringify(collision.exec('c')?.groups));
    console.log('escaped duplicate reference', flags, new RegExp('(?:(?<x>a)|(?<x>b))\\k<\\u0078>', flags).test('bb'));
}
console.log('literal duplicate', JSON.stringify(/(?<x>a)|(?<x>b)/du.exec('b')?.groups));
