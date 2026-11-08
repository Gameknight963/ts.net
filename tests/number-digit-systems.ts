export {};
for (const numberingSystem of ['beng', 'deva', 'thai', 'fullwide', 'hanidec']) {
    for (const notation of ['standard', 'scientific', 'engineering'] as const) {
        const options = {numberingSystem, notation, maximumFractionDigits: 2 as const};
        const format = new Intl.NumberFormat('en-US', options);
        console.log(numberingSystem, notation, format.resolvedOptions().numberingSystem);
        for (const value of [-12345.678, 0, -0, 0.00123, NaN, Infinity, -Infinity, 12345678901234567890n]) {
            const text = format.format(value);
            const parts = format.formatToParts(value);
            console.log(text, parts.map(part => part.type + ':' + part.value).join('|'), parts.map(part => part.value).join('') === text);
        }
        console.log('number', (12345.678).toLocaleString('en-US', options));
        console.log('bigint', (12345678901234567890n).toLocaleString('en-US', options));
    }
    for (const style of ['currency', 'percent'] as const) {
        const format = new Intl.NumberFormat('en-US', {numberingSystem, style, currency: 'USD'});
        console.log('style', style, format.format(-123.45), format.formatToParts(-123.45).map(part => part.type + ':' + part.value).join('|'));
    }
    let conversions = 0;
    const value = {valueOf() {conversions++; return 1234.5;}};
    const parts = new Intl.NumberFormat('en-US', {numberingSystem}).formatToParts(value as any);
    console.log('converted', conversions, parts.map(part => part.value).join(''));
}
