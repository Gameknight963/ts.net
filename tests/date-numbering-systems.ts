export {};
const date = new Date(Date.UTC(2024, 1, 29, 13, 4, 5, 678));
for (const numberingSystem of ['latn', 'arab', 'arabext', 'beng', 'deva', 'thai', 'fullwide', 'hanidec', 'mathbold', 'mathdbl', 'mathsans', 'mathsanb', 'mathmono', 'adlm', 'osma']) {
    const options: Intl.DateTimeFormatOptions = {numberingSystem, timeZone: 'UTC', year: 'numeric', month: '2-digit', day: '2-digit'};
    const format = new Intl.DateTimeFormat('en-US', options);
    console.log('date', numberingSystem, format.resolvedOptions().numberingSystem, format.format(date), date.toLocaleDateString('en-US', options));
    const locale = 'en-US-u-nu-' + numberingSystem;
    const localized = new Intl.DateTimeFormat(locale, {timeZone: 'UTC', year: 'numeric', month: '2-digit', day: '2-digit'});
    console.log('locale', localized.resolvedOptions().locale, localized.format(date));
    const timeOptions: Intl.DateTimeFormatOptions = {numberingSystem, timeZone: 'UTC', hour: '2-digit', minute: '2-digit', second: '2-digit', fractionalSecondDigits: 3};
    console.log('time', date.toLocaleTimeString('en-US', timeOptions).replace(/\s/g, ' '));
    console.log('override', new Intl.DateTimeFormat(locale, {...options, numberingSystem: 'latn'}).resolvedOptions().locale);
}
for (const numberingSystem of ['', 'ab', 'bad_name', 'latn--abc']) {
    try {new Intl.DateTimeFormat('en-US', {numberingSystem});}
    catch (error) {console.log('invalid', numberingSystem, (error as Error).name);}
}
