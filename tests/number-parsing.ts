const inputs = ['', '  ', '\uFEFF-0tail', '123tail', '-12.5e2x', '0x10', '-0Xff', '0b11', '+Infinity!', '-Infinity', 'infinity', '.5x', '-.5', '.', '1.e2', '1e+', '1e-2suffix', '1e999', '-1e-999', '9007199254740993', '18446744073709551617', 'zzzzzzzzzzzzzzzzzzzzz'];
for (const input of inputs) {
    const value = parseFloat(input);
    console.log('float', input, value, 1 / value);
    for (const radix of [undefined, 0, 2, 8, 10, 16, 36, 1, 37, 4294967298]) {
        const value = parseInt(input, radix);console.log('integer', input, radix, value, 1 / value);
    }
}
console.log('identity', Number.parseInt === parseInt, Number.parseFloat === parseFloat);
console.log('number variants', Number.parseInt('ff', 16), Number.parseFloat('2.5tail'));
const input = {toString(): string {console.log('input');return '123';}};
const radix = {valueOf(): number {console.log('radix');return 10;}};
console.log('order', parseInt(input as any, radix as any), parseFloat(input as any));
try {parseInt({toString(): never {throw 'input failure';}} as any, radix as any);} catch(error) {console.log('failure', error);}
console.log('overflow', parseInt('1'.repeat(2000), 2), parseInt('-' + '9'.repeat(2000), 10));
for (const value of [null, undefined, true, -0, 123.5, 1e21, 1e-7]) console.log('coerce', parseInt(value as any), parseFloat(value as any));
for (const method of [parseInt, parseFloat]) console.log('metadata', method.name, method.length, Object.keys(Number).includes(method.name));

const alphabet = '0123456789abcdefghijklmnopqrstuvwxyz';
for (let radix = 2; radix <= 36; radix++) {
    for (let length = 1; length <= 70; length += 3) {
        let digits = '';
        for (let index = 0; index < length; index++) digits += alphabet[(index * 17 + length * 7 + radix) % radix];
        console.log('sample', radix, length, parseInt(digits, radix), parseInt('000' + digits, radix), parseInt('-' + digits, radix));
    }
}
