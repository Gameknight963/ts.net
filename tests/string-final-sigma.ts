export {};
for (const text of ['\u039f\u03a3', '\u039f\u03a3\u0391', '\u03a3', 'A\u03a3', 'A\u03a31', 'A\u03a3-B', 'A\u03a3\u0301', 'A\u03a3\u0301B', "A'\u03a3", "A\u03a3'B", 'A\u03a3\u200dB', '\u{10400}\u03a3', '\u{1f600}\u03a3', 'A\u03a3\u00b7B']) {
    console.log('default', text, text.toLowerCase());
    for (const locale of ['en-US', 'el-GR', 'tr-TR']) console.log(locale, text.toLocaleLowerCase(locale));
}
for (const code of [0x27, 0x2e, 0x3a, 0xb7, 0x387, 0x55f, 0x5f4, 0x2018, 0x2019, 0x2024, 0x2027, 0xfe13, 0xfe52, 0xfe55, 0xff07, 0xff0e, 0xff1a]) {
    const character = String.fromCodePoint(code);
    console.log('ignored', code, ('A' + character + '\u03a3').toLowerCase(), ('A\u03a3' + character + 'B').toLowerCase());
}
for (const code of [0xaa, 0xba, 0x2160, 0x24b6, 0x1f130, 0x1f150, 0x1f170]) console.log('cased', code, (String.fromCodePoint(code) + '\u03a3').toLowerCase());
for (const text of ['A\u03a3\u03a3', 'A\u03a3\u0301\u03a3', 'A\u03a3\ud800', '\udc00\u03a3']) console.log('boundaries', text.toLowerCase());
