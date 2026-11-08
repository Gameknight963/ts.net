export {};
const input = [
 'A', 'a', '\u01C5', '\u02B0', '\u4E00', '\u0301', '\u0903', '\u20DD',
 '0', '\u0665', '\u2160', '\u00BD', '_', '-', '(', ')', '\u2018', '\u2019', '!',
 '+', '$', '^', '\u{1F600}', ' ', '\n', '\r', '\u0085', '\u00A0', '\u2028', '\u2029',
 '\u200C', '\u200D', '\u2066', '\u061C', '\uFEFF', '\uE000', '\u0378', '\uD800', '\uDC00',
 '\uFFFF', '\uFDD0', '\u{10FFFF}', '\u{10400}', '\u{1D7CE}', '\u{1F1E6}', '\u{1F3FF}', '\uFE0F', '\u{E0100}',
 '\u180F', '\uFF10', '\uFF21', '\uFF41', '\u{10940}'
].join('|');
for (const property of [
 'Lu', 'Ll', 'Lt', 'Lm', 'Lo', 'Mn', 'Mc', 'Me', 'Nd', 'Nl', 'No', 'Pc', 'Pd', 'Ps', 'Pe', 'Pi', 'Pf', 'Po', 'Sm', 'Sc', 'Sk', 'So',
 'Zs', 'Zl', 'Zp', 'Cc', 'Cf', 'Cs', 'Co', 'Cn', 'L', 'LC', 'M', 'N', 'P', 'S', 'Z', 'C',
 'Letter', 'Cased_Letter', 'Combining_Mark', 'Number', 'Punctuation', 'Uppercase_Letter', 'Lowercase_Letter', 'General_Category=Letter', 'gc=Nd',
 'Any', 'ASCII', 'Assigned', 'ASCII_Hex_Digit', 'AHex', 'Hex_Digit', 'Hex', 'White_Space', 'space', 'Pattern_White_Space', 'Pat_WS',
 'Join_Control', 'Join_C', 'Bidi_Control', 'Bidi_C', 'Regional_Indicator', 'RI', 'Emoji_Modifier', 'EMod', 'Variation_Selector', 'VS', 'Noncharacter_Code_Point', 'NChar'
]) {
 for (const pattern of ['\\p{' + property + '}+', '\\P{' + property + '}+', '[\\p{' + property + '}0]+', '[^\\p{' + property + '}]+']) {
  const regex = new RegExp(pattern, 'dgu');
  console.log('property', pattern, JSON.stringify([...input.matchAll(regex)].map(match => [match[0], match.index, (match as any).indices])));
 }
}
console.log('literal property', /\p{Letter}+/u.exec('123Hello\u{10400}456')?.[0]);
for (const pattern of ['\\p', '\\p{}', '\\p{letter}', '\\p{Letter=}', '\\p{Unknown}', '\\p{gc=Wrong}', '\\p{gc=Lu=Ll}', '\\p{gc=Lu', '[a-\\p{Zl}]', '[\\p{Zl}-\\p{Zl}]']) {
 try {new RegExp(pattern, 'u'); console.log('accepted invalid property', pattern);}
 catch (error) {console.log('property syntax', pattern, (error as Error).name);}
}
