export {};
for (const locale of ['en', 'en-u-nu-deva', 'en-u-nu-latn', 'en-u-nu-foobar']) {
    for (const numberingSystem of [undefined, 'foobar', 'DEVA', 'latn', 'deva', 'bali']) {
        const number = new Intl.NumberFormat(locale, {numberingSystem});
        const date = new Intl.DateTimeFormat(locale, {numberingSystem, timeZone: 'UTC', year: 'numeric'});
        const relative = new Intl.RelativeTimeFormat(locale, {numberingSystem} as any);
        console.log('number', locale, numberingSystem, number.resolvedOptions().locale, number.resolvedOptions().numberingSystem, number.format(1234));
        console.log('date', locale, numberingSystem, date.resolvedOptions().locale, date.resolvedOptions().numberingSystem, date.format(0));
        console.log('relative', locale, numberingSystem, relative.resolvedOptions().locale, relative.resolvedOptions().numberingSystem, relative.format(2, 'day'));
    }
}
console.log('locale methods', (1234).toLocaleString('en-US', {numberingSystem: 'foobar'}), new Date(0).toLocaleDateString('en-US', {timeZone: 'UTC', numberingSystem: 'foobar', year: 'numeric'}));
for (const constructor of [Intl.NumberFormat, Intl.DateTimeFormat, Intl.RelativeTimeFormat] as any[]) {
    let trace = '';
    const options = {get numberingSystem() {trace += 'get|'; return {toString() {trace += 'string|'; return 'foobar';}};}};
    const value = new constructor('en-US', options);
    console.log('coercion', constructor.name, trace, value.resolvedOptions().numberingSystem);
    for (const numberingSystem of ['', 'bad_system', 'ab', Symbol('system')]) {
        try {new constructor('en-US', {numberingSystem});} catch (error) {console.log('invalid', constructor.name, (error as Error).name);}
    }
}
