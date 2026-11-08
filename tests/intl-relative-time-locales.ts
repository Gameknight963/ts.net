export {};
for (const locale of ['es', 'es-ES', 'de', 'de-DE', 'fr', 'fr-FR', 'ja', 'ja-JP', 'ko', 'ko-KR', 'zh', 'zh-CN', 'zh-TW', 'zh-Hans', 'zh-Hant', 'zh-Hant-CN', 'zh-Hans-TW']) {
    for (const style of ['long', 'short', 'narrow']) {
        for (const numeric of ['always', 'auto']) {
            const formatter = new Intl.RelativeTimeFormat(locale, {style, numeric} as any);
            console.log('options', JSON.stringify(formatter.resolvedOptions()));
            for (const unit of ['year', 'quarter', 'month', 'week', 'day', 'hour', 'minute', 'second']) {
                for (const value of [-2, -1, -0.5, -0, 0, 0.5, 1, 1.5, 2, 1234.5]) console.log('relative', unit, String(value), formatter.format(value, unit as any), JSON.stringify(formatter.formatToParts(value, unit as any)));
            }
        }
    }
    for (const numberingSystem of ['deva', 'mathbold', 'bali']) {
        const formatter = new Intl.RelativeTimeFormat(locale, {numberingSystem} as any);
        console.log('digits', numberingSystem, formatter.format(1.5, 'day'), JSON.stringify(formatter.formatToParts(1.5, 'day')), formatter.format(1234, 'year'));
    }
}
console.log('supported', Intl.RelativeTimeFormat.supportedLocalesOf(['de-DE', 'fr-FR', 'zz']).join(','));
console.log('fallback', new Intl.RelativeTimeFormat(['zz', 'fr-FR']).resolvedOptions().locale);
