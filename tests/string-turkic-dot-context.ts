export {};
for (const locale of ['tr-TR', 'az-AZ', 'en-US']) for (const text of ['I\u0307', 'I\u0323\u0307', 'I\u0315\u0307', 'I\u0301\u0307', 'I\u0340\u0307', 'I\u0345\u0307', 'I\u034f\u0307', 'J\u0307', 'i\u0307', '\u0130\u0307', 'I\u0307I\u0307', 'I\u0307A', 'AI\u0323\u0307B']) console.log(locale, text, text.toLocaleLowerCase(locale));
for (const code of [0x334, 0x323, 0x301, 0x315, 0x345, 0x34f, 0x1d165]) {
    const mark = String.fromCodePoint(code);
    for (const text of ['I' + mark + '\u0307', 'I' + mark + '\u0307\u0307']) console.log('mark', code, text.toLocaleLowerCase('tr-TR'));
}
console.log('surrogate', 'I\ud800\u0307'.toLocaleLowerCase('tr-TR'));
