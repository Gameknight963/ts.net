export {};
const properties = ['Basic_Emoji', 'Emoji_Keycap_Sequence', 'RGI_Emoji_Modifier_Sequence', 'RGI_Emoji_Flag_Sequence', 'RGI_Emoji_Tag_Sequence', 'RGI_Emoji_ZWJ_Sequence', 'RGI_Emoji'];
const input = [
    'A', '1', '#', '\u00A9', '\u00A9\uFE0F', '\u{1F600}', '\u{1FAEA}',
    '1\uFE0F\u20E3', '#\uFE0F\u20E3', '1\u20E3',
    '\u{1F44D}', '\u{1F44D}\u{1F3FD}', '\u{1F3FD}',
    '\u{1F1FA}\u{1F1F8}', '\u{1F1EF}\u{1F1F5}', '\u{1F1FA}', '\u{1F1E6}\u{1F1E6}',
    '\u{1F3F4}\u{E0067}\u{E0062}\u{E0065}\u{E006E}\u{E0067}\u{E007F}',
    '\u{1F468}\u200D\u{1F469}\u200D\u{1F467}',
    '\u{1F469}\u{1F3FD}\u200D\u{1F4BB}', '\u{1F469}\u200D', '\uD800', '\uDC00'
].join('|');
for (const property of properties) for (const flags of ['dgv', 'dgiv']) {
    for (const pattern of [
        '\\p{' + property + '}+', '[\\p{' + property + '}]+',
        '[\\p{' + property + '}--\\q{\u{1F600}|\u{1F44D}\u{1F3FD}}]+',
        '[\\p{' + property + '}&&\\q{\u{1F600}|\u{1F44D}\u{1F3FD}}]+'
    ]) {
        const regex = new RegExp(pattern, flags);
        console.log('string property', property, flags, pattern, JSON.stringify([...input.matchAll(regex)].map(match => [match[0], match.index, (match as any).indices])));
        console.log('replace', input.replace(regex, '<$&>'));
        console.log('split', JSON.stringify(input.split(regex)));
    }
    for (const pattern of ['\\P{' + property + '}', '[^\\p{' + property + '}]', '[^\\p{' + property + '}--\\p{' + property + '}]']) {
        try {new RegExp(pattern, flags); console.log('unexpected string property complement', property);}
        catch (error) {console.log('string property syntax', property, (error as Error).name);}
    }
    try {new RegExp('\\p{' + property + '}', 'u'); console.log('unexpected u string property', property);}
    catch (error) {console.log('string property requires v', property, (error as Error).name);}
}
console.log('bounded intersection', JSON.stringify(input.match(new RegExp('[^\\p{RGI_Emoji}&&\\p{ASCII}]', 'gv'))));
const doubled = new RegExp('(?<emoji>\\p{RGI_Emoji})\\k<emoji>', 'div').exec('\u{1F600}\u{1F600}');
console.log('reference', JSON.stringify(doubled), JSON.stringify(doubled?.groups), JSON.stringify((doubled as any)?.indices));
