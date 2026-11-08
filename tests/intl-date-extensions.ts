export {};
for (const locale of ['en-US-u-hc-h11', 'en-US-u-hc-h12', 'en-US-u-hc-h23', 'en-US-u-hc-h24', 'en-US-u-ca-gregory-hc-h24-nu-latn']) {
    for (const override of [{}, {hourCycle: 'h12'}, {hour12: true}, {hour12: false}] as Intl.DateTimeFormatOptions[]) {
        const format = new Intl.DateTimeFormat(locale, {timeZone: 'UTC', hour: '2-digit', minute: '2-digit', ...override});
        const options = format.resolvedOptions();
        console.log(locale, options.locale, options.calendar, options.numberingSystem, options.hourCycle, options.hour12);
        for (const hour of [0, 1, 12, 23]) console.log('time', hour, format.format(Date.UTC(2024, 0, 1, hour, 4)).replace(/\s/g, ' '));
    }
}
