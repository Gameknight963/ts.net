export {};
for(const text of ['x','x0','xGG','x0G','u','u0','u00','u000','uZZZZ','u00G0','u{foo}']) {
 for(const pattern of ['\\'+text,'['+'\\'+text+']']) {
  const regex=new RegExp(pattern);
  console.log('identity',pattern,regex.source,regex.test(text));
 }
}
for(const pattern of ['\\x41','\\u0041','[\\x41]','[\\u0041]']) {
 const regex=new RegExp(pattern);console.log('valid',pattern,regex.test('A'),regex.test('x41'),regex.test('u0041'));
}
console.log('quantifier',new RegExp('^\\u{3}$').test('uuu'),new RegExp('^\\u{3}$').test('u{3}'));
console.log('following escape',new RegExp('^\\x\\d$').test('x5'),new RegExp('^\\u\\d$').test('u5'));
console.log('surrogate',new RegExp('\\uD83D\\uDE00').test('\ud83d\ude00'));
console.log('escaped slash',new RegExp('\\\\uZZZZ').test('\\uZZZZ'),new RegExp('\\\\xGG').test('\\xGG'));
