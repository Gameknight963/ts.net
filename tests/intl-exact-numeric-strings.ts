export {};
for (const options of [{}, {maximumFractionDigits: 20}, {style: 'percent'}, {style: 'currency', currency: 'USD'}, {notation: 'scientific', maximumSignificantDigits: 20}, {signDisplay: 'always', maximumFractionDigits: 3}, {numberingSystem: 'deva'}] as Intl.NumberFormatOptions[]) {
    const format = new Intl.NumberFormat('en-US', options);
    for (const value of ['9007199254740993', '-9007199254740993.125', '1.0000000000000000001', '.1234567890123456789', '1234567890123456789e-3', '0x20000000000001', '0b100000000000000000000000000000000000000000000000000001', '0o400000000000000001', '-0', '-1e-999', '1e999', 'bad', '', '  +123.4500  ']) {
        // Node keeps underflowing strings in scientific notation; the spec converts them to signed zero.
        if (value === '-1e-999' && options.notation === 'scientific') continue;
        console.log(value, (format.format as Function)(value), (format.formatToParts as Function)(value).map((part: any) => part.type + ':' + part.value).join('|'));
    }
}
let conversions = 0;
const value = {valueOf() {conversions++; return '9007199254740993';}};
const format = new Intl.NumberFormat('en-US');
console.log('object', (format.format as Function)(value), conversions);
console.log('parts object', (format.formatToParts as Function)(value).map((part: any) => part.value).join(''), conversions);
console.log('bigint object', (format.format as Function)({valueOf() {return 9007199254740993n;}}));
