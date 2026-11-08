export {};
for(const separator of ['\n','\r','\r\n','\u2028','\u2029',' ']) {
 for(const flags of ['','m','s','ms']) {
  for(const pattern of ['a.b','^a$','^b$','a$','^b','.$','^.$']) {
   const regex=new RegExp(pattern,flags);const text='a'+separator+'b';
   console.log(JSON.stringify(separator),flags,pattern,regex.test(text),regex.test('a'+separator),regex.test(separator+'b'));
  }
 }
 for(const pattern of ['(?s:a.b)','(?-s:a.b)','(?m:^b$)','(?-m:^b$)','(?s:a(?-s:.)b)','(?m:^a(?-m:$))','(?m:^a$)|^b$']) {
  const regex=new RegExp(pattern,'ms');console.log('inline',JSON.stringify(separator),pattern,regex.test('a'+separator+'b'));
 }
}
console.log('literal classes',/[$^.]+/.exec('$^.')?.[0],/\^\.\$/.test('^.$'));
console.log('global','a\rb\u2028c\u2029d\ne'.match(/^.$/gm)?.join(','));
console.log('absolute',/a$/.test('a\n'),/a$/.test('a\r'),/a$/.test('a\u2028'),/a$/.test('a'));
