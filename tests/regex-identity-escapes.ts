export {};
for(const letter of ['a','e','A','G','Z','z','p','P']) {
 for(const pattern of ['\\'+letter,'['+'\\'+letter+']','('+ '\\'+letter+')+']) {
  const regex=new RegExp(pattern,'g');
  console.log(pattern,regex.source,regex.test(letter),regex.lastIndex,regex.test('unrelated'));
 }
 const insensitive=new RegExp('\\'+letter,'i');console.log('case',letter,insensitive.test(letter.toLowerCase()),insensitive.test(letter.toUpperCase()));
 const escaped=new RegExp('\\\\'+letter);console.log('literal slash',letter,escaped.test('\\'+letter),escaped.test(letter));
}
for(const pattern of ['\\p{L}','\\P{Letter}','\\p{Script=Greek}']) {
 const regex=new RegExp(pattern);console.log('property text',pattern,regex.test(pattern.slice(1)),regex.test('abc'));
}
console.log('controls',/\a/.test('a'),/\a/.test('\x07'),/\e/.test('e'),/\e/.test('\x1b'));
console.log('replace','AGA'.replace(/\A/g,'x'),'p{L}'.replace(new RegExp('\\p{L}'),'letter'));
