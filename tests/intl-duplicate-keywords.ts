export {};
for (const tag of ['en-u-kn-false-kn-true', 'en-u-kn-kn-false', 'en-u-attr-ATTR-ca-gregory-ca-buddhist', 'en-u-nu-deva-nu-latn', 'en-u-nu-latn-nu-deva', 'en-u-zzz-aaa-zzz-kf-upper-kf-lower-kn']) {
    const locale = new Intl.Locale(tag);
    console.log('locale', locale.toString(), locale.numeric, locale.calendar, locale.numberingSystem, locale.caseFirst);
    console.log('canonical', Intl.getCanonicalLocales([tag, locale.toString()]).join('|'));
    console.log('options', new Intl.Locale(tag, {numeric: true, numberingSystem: 'beng'}).toString());
}
for (const tag of ['en-US-u-nu-deva-nu-latn', 'en-US-u-nu-latn-nu-deva']) {
    const format = new Intl.NumberFormat(tag);
    console.log('number', format.resolvedOptions().numberingSystem, format.format(1234));
}
for (const tag of ['en-US-u-kn-false-kn-true', 'en-US-u-kn-kn-false']) {
    const format = new Intl.Collator(tag);
    console.log('collator', format.resolvedOptions().numeric, format.compare('2', '10'));
}
for (const tag of ['en-u-attr-attr-u-kn', 'en-a-foo-a-bar', 'en-u-nu-latn-nu-bad_name']) {
    try {new Intl.Locale(tag);}
    catch (error) {console.log('invalid', (error as Error).name);}
}
