export {};
for (const locale of ['en-US-u-hc-h11', 'en-US-u-hc-h12', 'en-US-u-hc-h23', 'en-US-u-hc-h24', 'en-US-u-ca-gregory-hc-h24-nu-latn']) {
    for (const override of [{}, {hourCycle: 'h12'}, {hour12: true}, {hour12: false}] as Intl.DateTimeFormatOptions[]) {
        for (const hour of [0, 1, 12, 23]) {
            const date = new Date(Date.UTC(2024, 0, 1, hour, 4));
            const options = {timeZone: 'UTC', hour: '2-digit', minute: '2-digit', ...override} as Intl.DateTimeFormatOptions;
            console.log(locale, hour, date.toLocaleTimeString(locale, options).replace(/\s/g, ' '));
        }
    }
}
const date = new Date(Date.UTC(2024, 0, 1, 0, 4));
const locale = new Intl.Locale('en-US', {hourCycle: 'h24'});
console.log('object', date.toLocaleTimeString(locale, {timeZone: 'UTC', hour: '2-digit', minute: '2-digit'}));
console.log('fallback', date.toLocaleDateString(['zz-ZZ', 'en-US'], {timeZone: 'UTC', year: 'numeric'}));
let read = false;
try {date.toLocaleTimeString(['en-US', 'bad_locale'], {get hour() {read = true; return 'numeric' as const;}});}
catch (error) {console.log('invalid', (error as Error).name, read);}
