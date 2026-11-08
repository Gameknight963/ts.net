export {};
const locales = ['de-DE', 'fr-FR', 'ru-RU', 'ja-JP', 'ko-KR', 'zh-CN', 'zh-Hant'];
for (const locale of locales) {
    for (const type of ['cardinal', 'ordinal']) {
        for (const options of [{}, {minimumFractionDigits: 1}, {maximumFractionDigits: 0}, {minimumIntegerDigits: 3}, {minimumSignificantDigits: 2}, {maximumSignificantDigits: 2}, {maximumFractionDigits: 0, roundingMode: 'ceil'}, {minimumFractionDigits: 1, trailingZeroDisplay: 'stripIfInteger'}]) {
            const rules: any = new Intl.PluralRules(locale, {type, ...options} as any);
            console.log('options', JSON.stringify(rules.resolvedOptions()));
            for (const value of [-1000000, -21, -1.1, -1, -0, 0, 0.1, 1, 1.1, 1.5, 1.9999, 2, 3, 4, 5, 10, 11, 12, 14, 20, 21, 22, 25, 101, 111, 121, 1000000, 2000000, 1000000.1, 1e21, Number.MIN_VALUE, Number.MAX_VALUE, NaN, Infinity]) {
                // Node differs from the French rule for these huge values.
                if (locale === 'fr-FR' && Math.abs(value) >= 1e21 && Number.isFinite(value)) continue;
                console.log('select', value, rules.select(value));
            }
            for (const [first, last] of [[0, 2], [1, 2], [2, 1], [1, 1.5], [21, 2], [Infinity, 1], [1, Infinity]]) {
                // Equal rounded endpoints use the current specification's rule;
                // this Node build applies its older range mapping instead.
                const numbers = new Intl.NumberFormat('en-US', {...options, useGrouping: false} as any);
                if (Number.isFinite(first) && Number.isFinite(last) && numbers.format(first) === numbers.format(last)) continue;
                console.log('range', first, last, rules.selectRange(first, last));
            }
        }
    }
}
console.log('supported', Intl.PluralRules.supportedLocalesOf(locales).join(','));
console.log('locale fallback', new Intl.PluralRules(['zz', 'ru-RU']).resolvedOptions().locale);

for (const locale of ['de', 'fr', 'ru', 'ja', 'ko', 'zh']) {
    const rules: any = new Intl.PluralRules(locale);
    let reads = 0;
    console.log('coercion', locale, rules.select({valueOf() {reads++; return 1;}}), reads);
    try {rules.select(Symbol('value'));} catch (error) {console.log('symbol', locale, (error as Error).name);}
    try {rules.selectRange(NaN, 1);} catch (error) {console.log('range NaN', locale, (error as Error).name);}
}
