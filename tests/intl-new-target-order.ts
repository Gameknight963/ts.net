export {};
for (const constructor of [Intl.NumberFormat, Intl.DateTimeFormat, Intl.Collator, Intl.Segmenter, Intl.Locale, Intl.ListFormat, Intl.RelativeTimeFormat] as any[]) {
    for (const mode of ['normal', 'prototype error', 'invalid locale']) {
        let log = '';
        const prototype = {};
        function Alternate() {}
        const target = Alternate.bind(null);
        Object.defineProperty(target, 'prototype', {get() {log += 'P'; if (mode === 'prototype error') throw new Error('prototype'); return prototype;}});
        const locales: any = constructor === Intl.Locale ? {toString() {log += 'T'; return mode === 'invalid locale' ? 'bad_locale' : 'en-US';}} : {get length() {log += 'L'; return 1;}, get 0() {log += 'T'; return mode === 'invalid locale' ? 'bad_locale' : 'en-US';}};
        const options = {get language() {log += 'A'; return undefined;}, get localeMatcher() {log += 'M'; return undefined;}, get numberingSystem() {log += 'N'; return 'latn';}, get granularity() {log += 'G'; return 'grapheme';}};
        try {const result = Reflect.construct(constructor, [locales, options], target); console.log(constructor.name, mode, Object.getPrototypeOf(result) === prototype, log);}
        catch (error) {console.log(constructor.name, mode, (error as Error).name, log);}
    }
}
