function units(value: string): string {
    const codes: number[] = [];
    for (let index = 0; index < value.length; index++) codes.push(value.charCodeAt(index));
    return codes.join(',');
}
console.log('empty', String.fromCharCode().length, String.fromCodePoint().length);
console.log('ascii', String.fromCharCode(65, 66, 67), String.fromCodePoint(65, 66, 67));
for (const value of [-0, -1, -65537, 65535, 65536, 65537, NaN, Infinity, -Infinity, 1.9, 1e100]) console.log('unit', value, units(String.fromCharCode(value)));
for (const value of [0, 0xd7ff, 0xd800, 0xdbff, 0xdc00, 0xdfff, 0xe000, 0xffff, 0x10000, 0x1f600, 0x10ffff]) console.log('point', value, units(String.fromCodePoint(value)));
for (const value of [-1, 0x110000, 1.5, NaN, Infinity, -Infinity]) {
    try {String.fromCodePoint(value);} catch (error) {console.log('invalid', value, (error as Error).name);}
}
console.log('mixed', units(String.fromCodePoint(65, 0x1f600, 0xd800, 66)));
const first = {valueOf(): number {console.log('first');return 65;}};
const second = {valueOf(): number {console.log('second');return 66;}};
console.log('coercion', (String.fromCharCode as any)(first, second), (String.fromCodePoint as any)(first, second));
try {(String.fromCodePoint as any)(-1, second);} catch(error) {console.log('stopped', (error as Error).name);}
try {(String.fromCharCode as any)({valueOf(): never {throw 'conversion';}}, second);} catch(error) {console.log('stopped conversion', error);}
console.log('primitive', units((String.fromCharCode as any)(null, undefined, true, '67')), units((String.fromCodePoint as any)(null, false, '68')));
console.log('borrowed', String.fromCharCode.call(null, 69), String.fromCodePoint.call({}, 70));
for (const method of [String.fromCharCode, String.fromCodePoint]) console.log('metadata', method.name, method.length, Object.keys(String).includes(method.name));
for (let point = 0; point <= 0x10ffff; point += 9973) console.log('sample', point, units(String.fromCodePoint(point)));
