export {};
const locales = ['de', 'de-DE', 'fr', 'fr-FR', 'ru', 'ru-RU', 'ja', 'ja-JP', 'ko', 'ko-KR', 'zh', 'zh-CN', 'zh-TW', 'zh-Hans', 'zh-Hant'];
for (const locale of locales) {
    for (const type of ['conjunction', 'disjunction', 'unit']) {
        for (const style of ['long', 'short', 'narrow']) {
            const formatter = new Intl.ListFormat(locale, {type, style} as any);
            console.log('options', JSON.stringify(formatter.resolvedOptions()));
            for (const values of [[], ['A'], ['A', 'B'], ['A', 'B', 'C'], ['A', 'B', 'C', 'D'], ['', 'B', ''], ['', '', ''], ['A, B', 'C and D'], ['\u732b', '\u72ac', '\u9ce5']]) {
                console.log('list', JSON.stringify(values), formatter.format(values), JSON.stringify(formatter.formatToParts(values)));
            }
        }
    }
}
console.log('supported', Intl.ListFormat.supportedLocalesOf(locales).join(','));
console.log('fallback', new Intl.ListFormat(['zz', 'fr-FR']).resolvedOptions().locale, new Intl.ListFormat('de-DE-u-nu-deva').resolvedOptions().locale);
