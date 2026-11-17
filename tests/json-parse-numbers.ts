export {};
const tokens = ['0', '-0', '1', '-1', '999999999999999', '-999999999999999',
    '1000000000000000', '-1000000000000000', '9007199254740991', '9007199254740992',
    '9007199254740993', '-9007199254740993', '18446744073709551615', '18446744073709551616',
    '18446744073709551617', '9999999999999999999999999999999999999999',
    '0.0', '-0.0', '1.25', '-1.25', '123456789012345.75', '1234567890123456.75',
    '0e100', '-0e100', '1e-4000', '-1e-4000', '1e4000', '-1e4000', '5e-324'];
let state = 246813579;
for (let length = 1; length <= 32; length++) {
    let digits = '';
    for (let i = 0; i < length; i++) {
        state = (Math.imul(state, 1664525) + 1013904223) >>> 0;
        digits += String(i === 0 ? 1 + state % 9 : state % 10);
    }
    tokens.push(digits, '-' + digits, digits + '.125', '-' + digits + 'e-5');
}
tokens.push('9'.repeat(1000), '-' + '9'.repeat(1000));
for (const token of tokens) {
    const value = JSON.parse(token);
    const array = JSON.parse('[' + token + ']');
    const record = JSON.parse('{"value":' + token + '}');
    if (!Object.is(value, array[0]) || !Object.is(value, record.value)) throw new Error('Number differs between containers');
    console.log(token.length, value, 1 / value);
}
for (const token of ['-', '+1', '01', '-01', '00', '-00', '1.', '-1.', '1e', '1e+', '1e-', '1x', '1 2', '1.e2']) {
    let invalid = false;
    try { JSON.parse(token); }
    catch (error) { invalid = (error as Error).name === 'SyntaxError'; }
    if (!invalid) throw new Error('Invalid number accepted');
}
const source = '-999999999999999';
JSON.parse(source, (key: string, value: any, context?: any): any => {
    if (context.source !== source) throw new Error('Reviver source changed');
    console.log('source', context.source, value);
    return value;
});
