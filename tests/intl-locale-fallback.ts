export {};
for (const constructor of [Intl.Collator, Intl.NumberFormat, Intl.DateTimeFormat, Intl.Segmenter] as any[]) {
    const supported = constructor.supportedLocalesOf(['zz-ZZ', 'en-US', 'EN-us', 'de-DE-u-nu-deva']);
    console.log('supported', constructor.name, supported.join('|'));
    console.log('object', constructor.name, constructor.supportedLocalesOf(new Intl.Locale('en-US')).join('|'));
    let reads = 0;
    try {constructor.supportedLocalesOf(['en-US', 'bad_locale'], {get localeMatcher() {reads++; throw new Error('option read');}});}
    catch (error) {console.log('validation', constructor.name, (error as Error).name, reads);}
}
for (const locales of [['zz-ZZ', 'de-DE'], ['zz-ZZ', 'en-US-u-nu-deva'], ['zz-ZZ', 'de-DE', 'en-US']]) {
    const format = new Intl.NumberFormat(locales);
    console.log('fallback', format.resolvedOptions().locale, format.resolvedOptions().numberingSystem, format.format(12345.67));
    console.log('number', (12345.67).toLocaleString(locales));
    console.log('bigint', (1234567890n).toLocaleString(locales));
}
