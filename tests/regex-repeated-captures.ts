export {};
const patterns = [
    '(a|(b))+', '((a)?b)+', '((a)+|b)+', '(?:(?<a>a)|(?<b>b))+',
    '(?:(?<a>a)|(?<b>b))+\\k<a>', '((a)|(b))+\\2',
    '((a)|(b))+a', '((a)?b)+b', '((a)?b){1,3}', '((a)?b)+?',
    '(?:(?=(a))a|b)+', '(?:(?!(a))b|a)+', '(?<a>a\\k<a>?)+',
    '(?<=((a)|b)+)c', '(?<=((a)+|b)+)c',
    '(?<=((?=(a))a|b)+)c', '(?=(?:(a)|b)+c).+',
    '((?:a|(b)){1,3})+', '(((a)?b)+|c)+'
];
for (const flags of ['dg', 'dgu', 'dgv', 'dgiu', 'dgiv']) {
    for (const pattern of patterns) {
        for (const text of ['a', 'b', 'ab', 'ba', 'aba', 'bab', 'aab', 'abb', 'abab', 'ababa', 'abc', 'bac', 'aabc', 'bbac', 'abcb', 'ABa']) {
            const regex = new RegExp(pattern, flags);
            console.log('repeated', flags, pattern, text, JSON.stringify([...text.matchAll(regex)].map(match => [match[0], match.slice(1), match.index, match.groups, (match as any).indices])));
        }
    }
}
for (const pattern of ['(?:(?<a>a)|(?<b>b))+', '((a)?b)+', '(?<=((a)|b)+)c']) {
    const regex = new RegExp(pattern, 'dgv');
    const text = 'ab aba bac abc abb';
    console.log('replacement', pattern, text.replace(regex, '$&:$1:$2:$<a>:$<b>'));
    console.log('callback', pattern, text.replace(regex, (...args: any[]) => JSON.stringify(args)));
    console.log('split', pattern, JSON.stringify(text.split(regex)));
}
for (const flags of ['', 'g', 'y', 'u', 'gu', 'yu', 'iv', 'giv', 'yiv']) {
    for (const pattern of ['(?:(?<a>a)|(?<b>b))+', '((a)?b)+', '(?:(?<a>a)|(?<b>b))+\\k<a>', '((a)|(b))+\\2']) {
        for (const position of [0, 1, 3]) {
            const regex = new RegExp(pattern, flags);
            regex.lastIndex = position;
            console.log('test capture state', flags, pattern, position, regex.test('abba'), regex.lastIndex);
        }
    }
}
for (const pattern of ['(a)**', '(a)+*', '(a){2}*', '(a)+??']) {
    try {new RegExp(pattern); console.log('unexpected repeated quantifier', pattern);}
    catch (error) {console.log('quantifier syntax', pattern, (error as Error).name);}
}
