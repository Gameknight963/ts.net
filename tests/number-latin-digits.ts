export {};
for (const locale of ['en-US', 'de-DE', 'ja-JP']) {
    const options = {numberingSystem: 'latn', maximumFractionDigits: 2 as const};
    const format = new Intl.NumberFormat(locale, options);
    console.log(locale, format.resolvedOptions().numberingSystem, format.format(-12345.678));
    console.log('parts', format.formatToParts(12345.67).map(part => part.type + ':' + part.value).join('|'));
    console.log('number', (12345.678).toLocaleString(locale, options));
    console.log('bigint', (12345678901234567890n).toLocaleString(locale, options));
}
for (const system of ['', 'a', 'ab', 'abcdefghi', 'latn_', 'latn--abc', 'latn-ab', '123456789']) {
    try {new Intl.NumberFormat('en-US', {numberingSystem: system});}
    catch (error) {console.log('invalid', system, (error as Error).name);}
}
let log = '';
const options = {
    get numberingSystem() {log += 'N'; return {toString() {log += 'T'; return 'latn';}};},
    get style() {log += 'S'; return 'decimal' as const;}
};
console.log('coerced', new Intl.NumberFormat('en-US', options as any).format(1234), log);
