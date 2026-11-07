function hash(value: string): number {
    let result = value.length;
    for (let index = 0; index < value.length; index++) result = (result * 31 + value.charCodeAt(index)) | 0;
    return result;
}
const samples = ['', 'abc', 'aaaa', 'a,b,a,', '\uD83D\uDE00x', '\uD800a', 'x\u0000x', '$&a'];
const searches = ['', 'a', 'aa', ',', 'absent', '\uD83D'];
const replacements = ['', 'X', '$$', '$&', '$`', "$'", '$1-$01-$<name>', '$$$&', 'tail$', '\uDC00'];
for (let sample = 0; sample < samples.length; sample++) {
    for (let search = 0; search < searches.length; search++) {
        for (let replacement = 0; replacement < replacements.length; replacement++) {
            console.log('matrix', sample, search, replacement,
                hash(samples[sample].replace(searches[search], replacements[replacement])),
                hash(samples[sample].replaceAll(searches[search], replacements[replacement])));
        }
    }
}
console.log('first', 'aba'.replace('a', 'x'), 'aba'.replaceAll('a', 'x'));
console.log('callback', 'aba'.replaceAll('a', function(this: unknown, match: string, offset: number, source: string): string {
    console.log('call', this === undefined, match, offset, source); return '[' + offset + ']';
}));
console.log('empty callback', 'ab'.replaceAll('', (_match, offset) => String(offset)));
const replacement = {toString(): string {console.log('replacement conversion');return '$&';}};
console.log('returned object', 'aa'.replaceAll('a', (() => replacement) as any));
const receiver = {toString(): string {console.log('receiver conversion');return 'abc';}};
const search = {toString(): string {console.log('search conversion');return 'absent';}};
const replace = String.prototype.replace as any;
const replaceAll = String.prototype.replaceAll as any;
console.log('order', replaceAll.call(receiver, search, replacement));
console.log('generic', replace.call(123, '2', 'x'), replaceAll.call(false, 'a', 'X'));
console.log('missing', replace.call('undefined', undefined, undefined), replaceAll.call('nullnull', null, 7));
try {replaceAll.call(null, search, replacement);} catch(error) {console.log('null', (error as Error).name);}
try {'aaa'.replaceAll('a', () => {throw 'callback failure';});} catch(error) {console.log('failure', error);}
for (const method of [replace, replaceAll]) console.log('metadata', method.name, method.length, Object.keys(String.prototype).includes(method.name));
