export {};
for (const text of ['\ufb00\ufb01\ufb02\ufb03\ufb04\ufb05\ufb06', '\u0149\u01f0', 'Stra\u00dfe', '\u00df\u00df', '\u0130', 'I\u0130\u0131i', 'a\u0130z', '\u1e9e', '\u{1f600}\u00df\u0130']) {
    console.log('default', text, text.toUpperCase(), text.toLowerCase());
    for (const locale of ['en-US', 'de-DE', 'tr-TR', 'az-AZ']) console.log(locale, text.toLocaleUpperCase(locale), text.toLocaleLowerCase(locale));
}
