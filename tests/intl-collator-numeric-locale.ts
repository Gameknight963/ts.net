export {};
for (const locale of ['en-US-u-kn', 'en-US-u-kn-true', 'en-US-u-kn-false', 'en-US-u-kn-unknown', 'en-US-u-kn-nu-thai']) {
    for (const numeric of [undefined, true, false]) {
        const format = new Intl.Collator(locale, {numeric});
        const options = format.resolvedOptions();
        console.log(locale, numeric, options.locale, options.numeric, format.compare('2', '10'), format.compare('a9', 'a10'));
        console.log('string', '2'.localeCompare('10', locale, {numeric}));
    }
    console.log('default string', '2'.localeCompare('10', locale));
}
let calls = 0;
const options = {get numeric() {calls++; return false;}};
console.log('getter', new Intl.Collator('en-US-u-kn', options).compare('2', '10'), calls);
const locale = new Intl.Locale('en-US', {numeric: true});
console.log('locale object', '2'.localeCompare('10', locale), '2'.localeCompare('10', locale, {numeric: false}));
console.log('case locale', 'i'.toLocaleUpperCase(new Intl.Locale('tr-TR')));
