export {};
for (const locale of ['en-US-u-nu-deva', 'de-DE-u-nu-fullwide', 'en-US-u-ca-gregory-nu-thai', 'en-US-u-nu-mathbold']) {
    for (const numberingSystem of [undefined, 'latn', 'beng', locale.includes('deva') ? 'deva' : 'thai']) {
        const format = new Intl.NumberFormat(locale, {numberingSystem});
        console.log(locale, numberingSystem, format.resolvedOptions().locale, format.resolvedOptions().numberingSystem, format.format(12345.67));
    }
    console.log('number', (12345.67).toLocaleString(locale));
    console.log('bigint', (1234567890n).toLocaleString(locale));
}
const selected = new Intl.Locale('en-US', {numberingSystem: 'hanidec'});
const format = new Intl.NumberFormat(selected);
console.log('locale object', format.resolvedOptions().locale, format.format(1234));
for (const locales of [['en-US', 'invalid_locale'], ['en-US', 3] as any]) {
    try {new Intl.NumberFormat(locales);}
    catch (error) {console.log('later locale', (error as Error).name);}
}
const override = new Intl.NumberFormat('en-US-u-nu-arab', {numberingSystem: 'deva'});
console.log('override', override.resolvedOptions().locale, override.resolvedOptions().numberingSystem, override.format(1234));
