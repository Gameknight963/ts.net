export {};
for (const locale of ['en-US', 'fr', 'de', 'ja', 'zh', 'th', 'ar', 'hi']) {
    for (const granularity of ['word', 'sentence'] as const) {
        const formatter = new Intl.Segmenter(locale, {granularity});
        console.log('resolved', JSON.stringify(formatter.resolvedOptions()));
        for (const text of [
            '', 'Hello, world! Next sentence. Yes?', "Don't stop. Mr. Smith arrived. Wow!",
            '3.14 1,234.50 42°C', 'e-mail foo_bar rock\u2019n\u2019roll',
            'one\u0000two\r\nthree', 'a\u0301 \u{1F469}\u200D\u{1F4BB}! \uD800x',
            '\u4F60\u597D\u4E16\u754C\u3002\u3053\u3093\u306B\u3061\u306F\u4E16\u754C\u3002',
            '\u0E20\u0E32\u0E29\u0E32\u0E44\u0E17\u0E22',
            '\u0645\u0631\u062D\u0628\u0627 \u0628\u0627\u0644\u0639\u0627\u0644\u0645.',
            '\u0928\u092E\u0938\u094D\u0924\u0947 \u0926\u0941\u0928\u093F\u092F\u093E\u0964'
        ]) {
            const segments = formatter.segment(text);
            console.log('segments', locale, granularity, JSON.stringify([...segments].map(part => [part.segment, part.index, part.input === text, part.isWordLike, Object.keys(part)])));
            for (const index of [-1, 0, 1, 2, 4, text.length - 1, text.length, 1.9, NaN, Infinity]) {
                const part = segments.containing(index);
                console.log('containing', String(index), JSON.stringify(part && [part.segment, part.index, part.isWordLike]));
            }
            const first = segments[Symbol.iterator](), second = segments[Symbol.iterator]();
            console.log('independent', JSON.stringify(first.next()), JSON.stringify(second.next()));
            while (!first.next().done) {}
            console.log('done', first.next().done, first.next().done, first[Symbol.iterator]() === first);
        }
    }
}
let reads = 0, strings = 0;
const cached = new Intl.Segmenter('en-US', {get granularity() {reads++; return 'word' as const;}});
const converted = cached.segment({toString() {strings++; return 'hello world';}} as any);
console.log('conversions', reads, strings, JSON.stringify(converted.containing({valueOf() {strings++; return 7;}} as any)), strings);
class CustomWords extends Intl.Segmenter {marker = 3;}
const child = new CustomWords('en-US', {granularity: 'word'});
console.log('derived words', child.marker, child instanceof CustomWords, child instanceof Intl.Segmenter, JSON.stringify([...child.segment('hello world')]));
Object.freeze(cached);
const frozen = Object.freeze(cached.segment('hello world'));
console.log('frozen segments', JSON.stringify([...frozen]), JSON.stringify(cached.resolvedOptions()));
