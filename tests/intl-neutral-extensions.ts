export {};
for (const tag of ['de-u-co-phonebk', 'es-u-co-trad', 'de-u-co-phonebk-kf-upper-kn', 'en-u-nu-mathbold', 'de-a-foobar-u-co-phonebk-x-private']) {
    const locale = new Intl.Locale(tag);
    console.log('canonical', locale.toString(), Intl.getCanonicalLocales(tag).join('|'));
    if (tag.startsWith('en')) {
        console.log('number', new Intl.NumberFormat(tag).format(1234));
    } else {
        const format = new Intl.Collator(tag);
        console.log('collator', format.resolvedOptions().collation, format.compare('ae', '\u00e4'), format.compare('ch', 'cz'));
        console.log('string', 'ae'.localeCompare('\u00e4', tag), 'ae'.localeCompare('\u00e4', [tag]));
    }
}
for (const tag of ['de-u-co-phonebk_', 'en-u-nu-abcdefghi', 'en-u-12-test', 'en-u-nu-', 'en-x-', 'en-a-abcdefghi', 'en-a-foo-a-bar', 'en-u-nu-latn-u-kn']) {
    try {new Intl.Locale(tag);}
    catch (error) {console.log('invalid', tag, (error as Error).name);}
}
