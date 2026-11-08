export {};
for (const disabled of [null, undefined]) {
    const regex: any = /a/g;
    regex.lastIndex = 1;
    regex[Symbol.replace] = disabled;
    regex[Symbol.split] = disabled;
    console.log('replace', 'a /a/g a /a/g'.replace(regex, 'X'));
    console.log('replaceAll', 'a /a/g a /a/g'.replaceAll(regex, 'X'));
    console.log('split', JSON.stringify('a /a/g b /a/g'.split(regex)), regex.lastIndex);
    regex[Symbol.match] = disabled;
    regex[Symbol.search] = disabled;
    regex[Symbol.matchAll] = disabled;
    console.log('match', JSON.stringify('aba'.match(regex)), regex.lastIndex);
    console.log('search', 'aba'.search(regex), regex.lastIndex);
    console.log('matchAll', Array.from('aba'.matchAll(regex), match => match.index).join(','), regex.lastIndex);
}
let effects = '';
const separator: any = /a/g;
separator[Symbol.split] = null;
separator[Symbol.replace] = null;
separator.toString = function () { effects += 'S'; return ','; };
const limit: any = {valueOf() {effects += 'L'; return 2;}};
console.log('order split', JSON.stringify('a,b,c'.split(separator, limit)), effects);
effects = '';
const replacement: any = {toString() {effects += 'R'; return 'x';}};
console.log('order replace', 'a,b,c'.replace(separator, replacement), effects);
