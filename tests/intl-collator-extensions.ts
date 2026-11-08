export {};
for (const locale of ['en-US-u-kf-upper', 'en-US-u-kf-lower-kn', 'en-US-u-kf-false', 'en-US-u-kf-invalid', 'de-AT-u-co-phonebk-kf-upper-kn', 'es-MX-u-co-trad']) {
    for (const options of [{}, {caseFirst: 'lower'}, {caseFirst: 'upper'}, {numeric: false}, {collation: 'default'}, {usage: 'search'}] as Intl.CollatorOptions[]) {
        const collator = new Intl.Collator(locale, options);
        const resolved = collator.resolvedOptions();
        console.log(locale, resolved.collation, resolved.caseFirst, resolved.numeric, resolved.usage);
        for (const pair of [['a', 'A'], ['2', '10'], ['a2', 'A10'], ['ae', '\u00e4'], ['ch', 'cz']]) {
            console.log('compare', collator.compare(pair[0], pair[1]), pair[0].localeCompare(pair[1], locale, options));
        }
    }
}
let reads = 0;
const options = {get caseFirst() {reads++; return 'lower' as const;}};
console.log('getter', new Intl.Collator('en-US-u-kf-upper', options).compare('a', 'A'), reads);
for (const locale of ['en-US-u-kf-upper', 'en-US-u-kf-lower-kn', 'en-US-u-kf-false', 'en-US-u-kf-invalid']) {
    console.log('locale', new Intl.Collator(locale).resolvedOptions().locale);
}
