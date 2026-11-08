export {};
const cases: [Intl.DisplayNamesOptions['type'], string[]][] = [
    ['language', ['en', 'en-US', 'en-GB', 'zh-Hans', 'sr-Latn', 'iw', 'xyz', 'abcdef', 'en-variant']],
    ['region', ['US', 'GB', '419', 'ZZ', 'QQ', 'BU']],
    ['script', ['Latn', 'Cyrl', 'Hans', 'Zyyy', 'Qaaa', 'Qaai']],
    ['currency', ['USD', 'EUR', 'JPY', 'XXX', 'XYZ', 'usd']],
    ['calendar', ['gregory', 'iso8601', 'buddhist', 'ethiopic', 'ethioaa', 'islamicc', 'foobar']],
    ['dateTimeField', ['era', 'year', 'quarter', 'month', 'weekOfYear', 'weekday', 'day', 'dayPeriod', 'hour', 'minute', 'second', 'timeZoneName']]
];
// Localized labels vary with platform data; compare API behavior here.
for (const locale of ['en-US', 'fr', 'de', 'ja', 'zh', 'ar']) for (const [type, codes] of cases) {
    for (const style of ['long', 'short', 'narrow'] as const) for (const fallback of ['code', 'none'] as const) {
        const formatter = new Intl.DisplayNames(locale, {type, style, fallback});
        console.log('display options', JSON.stringify(formatter.resolvedOptions()));
        console.log('display names', locale, type, style, fallback, JSON.stringify(codes.map(code => [code, formatter.of(code) === undefined ? undefined : formatter.of(code)!.length > 0])));
    }
}
for (const languageDisplay of ['dialect', 'standard'] as const) {
    const formatter = new Intl.DisplayNames('en', {type: 'language', languageDisplay});
    console.log('language display', languageDisplay, !!formatter.of('en-US'), !!formatter.of('zh-Hant'), !!formatter.of('sr-Latn-RS'));
}
const region = new Intl.DisplayNames('en-US', {type: 'region'});
console.log('metadata', Intl.DisplayNames.name, Intl.DisplayNames.length, region.of.name, region.of.length, region.resolvedOptions.name, region.resolvedOptions.length, Intl.DisplayNames.supportedLocalesOf.length);
console.log('tag', Object.prototype.toString.call(region));
console.log('supported', Intl.DisplayNames.supportedLocalesOf(['en-US', 'fr', 'en-US', 'zz']).join('|'));
const reads: string[] = [];
const options = {get localeMatcher() {reads.push('matcher'); return 'lookup' as const;},
    get style() {reads.push('style'); return 'short' as const;}, get type() {reads.push('type'); return 'region' as const;},
    get fallback() {reads.push('fallback'); return 'none' as const;}, get languageDisplay() {reads.push('language'); return 'standard' as const;}};
const cached = new Intl.DisplayNames('en-US', options);
cached.of('US'); cached.of('GB');
console.log('reads', reads.join(','));
let conversions = 0;
console.log('code conversion', region.of({toString() {conversions++; return 'US';}} as any), conversions);
class CustomNames extends Intl.DisplayNames {marker = 3;}
const child = new CustomNames('en-US', {type: 'region'});
console.log('derived', child.marker, child instanceof CustomNames, child instanceof Intl.DisplayNames, child.of('US'));
Object.freeze(region);
console.log('frozen', region.of('GB'), JSON.stringify(region.resolvedOptions()));
for (const method of [Intl.DisplayNames.prototype.of, Intl.DisplayNames.prototype.resolvedOptions]) {
    try {(method as any).call({}, 'US');} catch (error) {console.log('receiver', (error as Error).name);}
}
for (const options of [undefined, null, {}, {type: 'wrong'}, {type: 'region', style: 'wrong'}, {type: 'region', fallback: 'wrong'}, {type: 'region', languageDisplay: 'wrong'}]) {
    try {new Intl.DisplayNames('en-US', options as any);} catch (error) {console.log('invalid options', (error as Error).name);}
}
for (const [type, codes] of [
    ['language', ['', 'a', 'abcd', 'en_US', 'en-u-ca-gregory', 'en-x-private']],
    ['region', ['', 'USA', '12', '1A2']], ['script', ['abc', 'abcde']],
    ['currency', ['', 'US', '123', 'USDD']], ['calendar', ['', 'ab', 'gregorian', 'a-b']],
    ['dateTimeField', ['', 'Year', 'week', 'zone']]
] as const) {
    const formatter = new Intl.DisplayNames('en-US', {type});
    for (const code of codes) {
        try {formatter.of(code); console.log('unexpected display code', type, code);}
        catch (error) {console.log('invalid code', type, code, (error as Error).name);}
    }
}

for (const [type, code] of [['language','abcdef'],['region','QQ'],['script','Qaaa'],['currency','XYZ'],['calendar','foobar']] as const) {
    console.log('unknown fallback', type, new Intl.DisplayNames('en-US',{type,fallback:'code'}).of(code), new Intl.DisplayNames('en-US',{type,fallback:'none'}).of(code));
}
for (const [type, code] of [['language','en'],['region','US'],['script','Latn'],['currency','USD'],['calendar','buddhist'],['dateTimeField','year']] as const)
    console.log('common names',type,new Intl.DisplayNames('en-US',{type}).of(code));
console.log('script identity label',new Intl.DisplayNames('en-US',{type:'script',fallback:'none'}).of('Thai'));
