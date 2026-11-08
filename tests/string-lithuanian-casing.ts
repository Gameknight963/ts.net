export {};
for (const text of ['I\u0301', 'J\u0300', '\u012e\u0303', '\u00cc\u00cd\u0128', 'I\u0323\u0301', 'I\u034f\u0301', 'I\u0315', 'i\u0307', 'j\u0323\u0307', 'i\u0301\u0307', '\u012f\u0307', '\u{1d422}\u0307', 'I\u0307', '\u0130']) {
    console.log(text, text.toLocaleLowerCase('lt-LT'), text.toLocaleUpperCase('lt-LT'));
}
for (const code of [0x334, 0x323, 0x301, 0x315, 0x345, 0x34f, 0x1d165]) {
    const mark = String.fromCodePoint(code);
    console.log('mark', code, ('I' + mark + '\u0301').toLocaleLowerCase('lt-LT'), ('i' + mark + '\u0307').toLocaleUpperCase('lt-LT'));
}
