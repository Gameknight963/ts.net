export {};
// ICU can use narrow no-break spaces in parts but ordinary spaces in format.
function spacing(value: string) {return value.replace(/[\u00a0\u202f]/g, ' ');}
const timestamp = Date.UTC(2025, 11, 31, 13, 24, 56, 789);
for (const locale of ['en-US', 'de-DE']) {
    for (const numberingSystem of ['latn', 'deva', 'mathbold']) {
        for (const fields of [
            {year: 'numeric', month: '2-digit', day: '2-digit'},
            {weekday: 'long'},
            {hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23', fractionalSecondDigits: 3},
            {hour: 'numeric', minute: '2-digit', hourCycle: 'h12'},
            {year: 'numeric', month: 'long', day: 'numeric', hour: '2-digit', minute: '2-digit', hourCycle: 'h23'}
        ]) {
            const formatter = new Intl.DateTimeFormat(locale, {timeZone: 'UTC', numberingSystem, ...fields} as any);
            const parts = formatter.formatToParts(timestamp);
            console.log('fields', locale, numberingSystem, JSON.stringify(fields), JSON.stringify(parts.filter(part => part.type !== 'literal')));
            console.log('joined', spacing(parts.map(part => part.value).join('')) === spacing(formatter.format(timestamp)));
        }
    }
}
for (const dateStyle of [undefined, 'short', 'medium', 'long', 'full']) {
    for (const timeStyle of [undefined, 'short', 'medium', 'long', 'full']) {
        const formatter = new Intl.DateTimeFormat('en-US', {dateStyle, timeStyle, timeZone: 'UTC'} as any);
        const parts = formatter.formatToParts(timestamp);
        console.log('style', dateStyle, timeStyle, parts.length > 0, parts.every(part => typeof part.type === 'string' && typeof part.value === 'string'), spacing(parts.map(part => part.value).join('')) === spacing(formatter.format(timestamp)));
    }
}
const formatter = new Intl.DateTimeFormat('en-US', {timeZone: 'UTC', year: 'numeric'});
let conversions = 0;
console.log('coercion', formatter.formatToParts({valueOf() {conversions++; return timestamp;}} as any)[0].value, conversions);
console.log('metadata', formatter.formatToParts.name, formatter.formatToParts.length);
const method: any = Intl.DateTimeFormat.prototype.formatToParts;
try {method.call({}, {valueOf() {throw new Error('wrong order');}});} catch (error) {console.log('receiver', (error as Error).name);}
for (const value of [NaN, Infinity, 8640000000000001, 1n, Symbol('time')]) {
    try {method.call(formatter, value);} catch (error) {console.log('invalid', typeof value, (error as Error).name);}
}
const first = formatter.formatToParts(timestamp), second = formatter.formatToParts(timestamp);
first[0].value = 'changed';
console.log('fresh', first !== second, first[0] !== second[0], second[0].value === '2025');
console.log('properties', Object.keys(second[0]).join(','), Object.getOwnPropertyDescriptor(second[0], 'value')!.writable);

for (const options of [
    {year: 'numeric', month: '2-digit', day: '2-digit'},
    {hour: '2-digit', minute: '2-digit', second: '2-digit', hourCycle: 'h23', fractionalSecondDigits: 3}
]) {
    const formatter = new Intl.DateTimeFormat('en-US', {timeZone: 'UTC', ...options} as any);
    console.log('complete parts', JSON.stringify(formatter.formatToParts(timestamp)));
}

for (const locale of ['ru-RU', 'ja-JP', 'th-TH', 'ko-KR']) {
    for (const hourCycle of ['h11', 'h23']) {
        const formatter = new Intl.DateTimeFormat(locale, {dateStyle: 'full', timeStyle: 'long', hourCycle, timeZone: '+05:30'} as any);
        const parts = formatter.formatToParts(timestamp);
        console.log('calendar style', locale, hourCycle, spacing(parts.map(part => part.value).join('')) === spacing(formatter.format(timestamp)), parts.some(part => part.type === 'year'), parts.some(part => part.type === 'timeZoneName'));
    }
}
