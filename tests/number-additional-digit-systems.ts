export {};
const systems = ['ahom', 'bali', 'bhks', 'brah', 'cakm', 'cham', 'diak', 'gara', 'gong', 'gonm', 'gujr', 'gukh', 'guru', 'hmng', 'hmnp', 'java', 'kali', 'kawi', 'khmr', 'knda', 'krai', 'lana', 'lanatham', 'laoo', 'lepc', 'limb', 'mlym', 'modi', 'mong', 'mroo', 'mtei', 'mymr', 'mymrepka', 'mymrpao', 'mymrshan', 'mymrtlng', 'nagm', 'newa', 'nkoo', 'olck', 'onao', 'orya', 'outlined', 'rohg', 'saur', 'segment', 'shrd', 'sind', 'sinh', 'sora', 'sund', 'sunu', 'takr', 'talu', 'tamldec', 'telu', 'tibt', 'tirh', 'tnsa', 'tols', 'vaii', 'wara', 'wcho'];
for (const numberingSystem of systems) {
    for (const locale of ['en-US', 'de-DE']) {
        for (const options of [{}, {style: 'percent'}, {style: 'currency', currency: 'USD'}, {notation: 'scientific'}]) {
            if (locale === 'de-DE' && 'style' in options) continue;
            const formatter = new Intl.NumberFormat(locale, {...options, numberingSystem} as any);
            for (const value of [0, -0, 1234567890.125, -987.5, NaN, Infinity, -Infinity]) {
                console.log('number', numberingSystem, locale, JSON.stringify(options), String(value), formatter.format(value), JSON.stringify(formatter.formatToParts(value)));
            }
            console.log('resolved', formatter.resolvedOptions().numberingSystem);
        }
    }
    const date = new Intl.DateTimeFormat('en-US', {numberingSystem, timeZone: 'UTC', year: 'numeric', month: '2-digit', day: '2-digit'});
    console.log('date', numberingSystem, date.format(new Date(Date.UTC(2025, 11, 31))));
}
