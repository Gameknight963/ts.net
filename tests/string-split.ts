function show(text: string, separator: any, limit?: number): void {
    const values = text.split(separator, limit);
    console.log(values.length, values.join('|'));
}
show('a,b,,c,', ','); show('a,b,c', ',', 2); show('aaaaa', 'aa');
show('', ','); show('', ''); show('', undefined); show('abc', undefined);
show('anullb', null); show('abc', 'x'); show('abc', '', 2);
for (const limit of [0, -1, NaN, Infinity, 1.9, 4294967296, 4294967297]) show('a,b,c', ',', limit);
const units = '\uD83D\uDE00x'.split('');
for (const unit of units) console.log('unit', unit.charCodeAt(0));
console.log('generic', (String.prototype.split as any).call(123, '2').join('|'));
const split = String.prototype.split as any;
const receiver = {toString(): string {console.log('receiver');return 'a,b';}};
const separator = {toString(): string {console.log('separator');return ',';}};
const limit = {valueOf(): number {console.log('limit');return 0;}};
console.log('order', split.call(receiver, separator, limit).length);
try {split.call(null, separator, limit);} catch(error) {console.log('invalid receiver', (error as Error).name);}
console.log('metadata', split.name, split.length, Object.keys(String.prototype).includes('split'));

const samples = ['', 'a', 'a,b,,', 'aaaaa', 'a\u0000b', '\uD83D\uDE00x', '\uD800a', 'x\uDC00'];
const separators: any[] = ['', ',', 'aa', undefined, null, '\uD83D'];
const limits = [undefined, 0, 1, 2, -1, Infinity, NaN, 4294967296, 4294967297];
for (let sample = 0; sample < samples.length; sample++) {
    for (let separator = 0; separator < separators.length; separator++) {
        for (let limit = 0; limit < limits.length; limit++) {
            const values = samples[sample].split(separators[separator], limits[limit]);
            let hash = 0;
            for (const value of values) {
                hash = (hash * 31 + value.length) | 0;
                for (let index = 0; index < value.length; index++) hash = (hash * 31 + value.charCodeAt(index)) | 0;
            }
            console.log('matrix', sample, separator, limit, values.length, hash);
        }
    }
}
