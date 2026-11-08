export {};
for (const locale of ['en-US-u-nu-deva', 'en-US-u-ca-buddhist', 'en-US-u-hc-h24', 'en-US-u-foobar-nu-thai', 'en-US-x-private']) {
    const format = new Intl.Collator(locale);
    const options = format.resolvedOptions();
    console.log('collator', locale, options.locale, options.numeric, options.collation, format.compare('2', '10'), format.compare('a', 'b'));
}
for (const locale of ['en-US-u-kn', 'en-US-u-co-phonebk', 'en-US-u-kf-upper', 'en-US-u-foobar-kn-false', 'en-US-x-private']) {
    const format = new Intl.DateTimeFormat(locale, {timeZone: 'UTC', year: 'numeric', month: '2-digit', day: '2-digit'});
    console.log('date', locale, format.resolvedOptions().locale, format.format(0));
}
