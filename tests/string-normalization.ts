const samples = ['', 'plain', 'e\u0301', '\u00E9', '\u212B', '\uFB03', '\u2460', '\u1100\u1161\u11A8', '\uAC01', 'a\u0315\u0300', '\uD835\uDC00', '\uD83D\uDE00', '\uD800', '\uDC00', 'e\u0301\uD800e\u0301', '\uD800\uD800\uDC00\uDC00', '\uFFFE', '\uFFFF', '\uFDD0', '\uDBFF\uDFFF', 'a\u0000e\u0301'];
for (let point = 0xfdd0; point <= 0xfdef; point++) samples.push('e\u0301' + String.fromCodePoint(point) + '\u0301e\u0301');
for (let plane = 1; plane <= 16; plane++) {
    samples.push('e\u0301' + String.fromCodePoint(plane * 65536 + 0xfffe) + 'e\u0301');
    samples.push('e\u0301' + String.fromCodePoint(plane * 65536 + 0xffff) + 'e\u0301');
}
function units(value: string): string {
    const result: number[] = [];
    for (let index = 0; index < value.length; index++) result.push(value.charCodeAt(index));
    return result.join(',');
}
for (const form of ['NFC', 'NFD', 'NFKC', 'NFKD']) for (let index = 0; index < samples.length; index++) console.log(form, index, units(samples[index].normalize(form)));
console.log('default', units('e\u0301'.normalize()), units('e\u0301'.normalize(undefined)));
const method = String.prototype.normalize as any;
for (const form of ['', 'nfc', ' NFC', 'NFC ', null, 1]) {
    try {method.call('', form);} catch (error) {console.log('invalid form', (error as Error).name);}
}
console.log('generic', method.call(123), method.call(false));
const receiver = {toString(): string {console.log('receiver');return 'e\u0301';}};
const form = {toString(): string {console.log('form');return 'NFD';}};
console.log('order', units(method.call(receiver, form)));
try {method.call(null, form);} catch (error) {console.log('null receiver', (error as Error).name);}
try {method.call(receiver, {toString(): never {throw 'form failure';}});} catch (error) {console.log('failure', error);}
console.log('metadata', method.name, method.length, Object.keys(String.prototype).includes('normalize'));
