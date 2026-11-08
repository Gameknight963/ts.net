export {};
const properties = ["Alphabetic", "Alpha", "Bidi_Mirrored", "Bidi_M", "Case_Ignorable", "CI", "Cased", "Changes_When_Casefolded", "CWCF", "Changes_When_Casemapped", "CWCM", "Changes_When_Lowercased", "CWL", "Changes_When_NFKC_Casefolded", "CWKCF", "Changes_When_Titlecased", "CWT", "Changes_When_Uppercased", "CWU", "Dash", "Default_Ignorable_Code_Point", "DI", "Deprecated", "Dep", "Diacritic", "Dia", "Emoji", "Emoji_Component", "EComp", "Emoji_Modifier_Base", "EBase", "Emoji_Presentation", "EPres", "Extended_Pictographic", "ExtPict", "Extender", "Ext", "Grapheme_Base", "Gr_Base", "Grapheme_Extend", "Gr_Ext", "IDS_Binary_Operator", "IDSB", "IDS_Trinary_Operator", "IDST", "ID_Continue", "IDC", "ID_Start", "IDS", "Ideographic", "Ideo", "Logical_Order_Exception", "LOE", "Lowercase", "Lower", "Math", "Pattern_Syntax", "Pat_Syn", "Quotation_Mark", "QMark", "Radical", "Sentence_Terminal", "STerm", "Soft_Dotted", "SD", "Terminal_Punctuation", "Term", "Unified_Ideograph", "UIdeo", "Uppercase", "Upper", "XID_Continue", "XIDC", "XID_Start", "XIDS"];

const input = ['A','a','0','#','*','\u00AA','\u00B7','\u0301','\u0345','\u05B0','\u0627','\u061C','\u200C','\u200D','\uFE0F','\u{1F600}','\u{1F3FB}','\u{1F9B0}','\u{1F1E6}','\u{E0061}','\u{1FAEA}','\u{10940}','\uD800','\uDC00','(',')','+','\u2160','\u00AD','\uFEFF','\u2028','\u{2EBF0}'].join('|');
for (const property of properties) for (const pattern of ['\\p{' + property + '}+', '\\P{' + property + '}+', '[\\p{' + property + '}a]+', '[^\\p{' + property + '}]+']) {
 const regex = new RegExp(pattern, 'dgu');
 console.log('binary property', property, pattern, JSON.stringify([...input.matchAll(regex)].map(match => [match[0], match.index, (match as any).indices])));
}
console.log('literal emoji', /\p{Emoji}+/u.exec('Hello\u{1F600}\u{1FAEA}')?.[0]);
console.log('identifier', /^\p{ID_Start}\p{ID_Continue}*$/u.test('hello_1'), /^\p{XID_Start}\p{XID_Continue}*$/u.test('1hello'));
