export {};
for (const pattern of ['.', '.+', '^.$', '[^]', '[a-z]+', '[^a]', '[\\s\\S]', '[\\dA-Z]', '[^\\w]', '\\u{1F600}+', '[\\u{1F600}-\\u{1F64F}]+', '\\uD83D\\uDE00+', '[\\uD800-\\uDFFF]', '\\uD800', '\\uDC00', '(.)\\1', '(?<face>.)\\k<face>', '', '(?=.)', '(?<=.)']) {
 for (const flags of ['u', 'gu', 'su', 'du']) {
  const regex = new RegExp(pattern, flags);
  console.log('metadata', regex.source, regex.flags, regex.unicode);
  for (const text of ['', 'abc', 'A1 ', '\n', '\u2028', '\u{1F600}', 'a\u{1F600}b', '\u{1F600}\u{1F600}', '\uD800', '\uDC00', '\uD800a\uDC00']) {
   const match = regex.exec(text);
   console.log(pattern, flags, JSON.stringify(text), JSON.stringify(match), match?.index, JSON.stringify(match?.groups), JSON.stringify((match as any)?.indices), regex.lastIndex);
   regex.lastIndex = 0;
  }
 }
}
for (const text of ['', 'a\u{1F600}b', '\u{1F600}\u{1F600}', '\uD800a\uDC00']) for (const pattern of ['', '.', '(?=.)', '(.?)']) {
 const regex = new RegExp(pattern, 'gu');
 console.log('protocols', JSON.stringify(text), pattern, JSON.stringify(text.match(regex)), JSON.stringify([...text.matchAll(regex)].map(match => [match[0], match.index])), text.replace(regex, '#'), JSON.stringify(text.split(regex)));
}
for (const flags of ['gu', 'uy']) for (const index of [0, 1, 2, 3]) {
 const regex = new RegExp('.', flags); regex.lastIndex = index;
 const match = regex.exec('\u{1F600}a'); console.log('index', flags, index, match?.[0], match?.index, regex.lastIndex);
}
for (const pattern of ['\\a', '\\-', '\\u', '\\u{110000}', '\\u{}', '\\xG0', '[\\s-a]', '[z-a]', '\\1', '\\01', '{', '}', ']', '(?=a)?']) {
 try {new RegExp(pattern, 'u'); console.log('accepted invalid', pattern);}
 catch (error) {console.log('syntax', pattern, (error as Error).name);}
}
console.log('literals', /^.$/u.test('\u{1F600}'), /\u{1F600}+/u.exec('\u{1F600}\u{1F600}')?.[0]);
for (const pattern of ['[\\u{FFFF}-\\u{10401}]', '[\\u{10000}-\\u{10FFFF}]', '[^\\u{1F600}a]', '\\D', '\\S', '\\W', '\\B', '\\b', '(?s:.)', '(?m:^.$)', '(?:\u{1F600}|a){1,2}', '\\1(.)', '\\u{D83D}\\u{DE00}']) {
 for (const text of ['a\u{1F600}b', '\uFFFF', '\u{10000}', '\u{103FF}', '\u{10400}', '\u{10401}', '\u{10FFFF}', '\uD800\uD800\uDC00', '\uDFFF\uE000', '\n\u{1F600}\n', 'aa']) {
  const regex = new RegExp(pattern, 'gu');
  console.log('boundaries', pattern, JSON.stringify(text), JSON.stringify([...text.matchAll(regex)].map(match => [match[0], match.index])));
 }
}
for (const pattern of ['\\k<face>(?<face>.)', '(a|(b))\\2', '(?<a>a)|(b)\\2', '(a)(?<b>b)\\1\\2', '(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)\\10']) {
 for (const text of ['a', 'b', 'aa', 'bb', 'abab', 'abcdefghijj', '\u{1F600}']) {
  const regex = new RegExp(pattern, 'du'); const match = regex.exec(text);
  console.log('references', pattern, text, JSON.stringify(match), JSON.stringify(match?.groups), JSON.stringify((match as any)?.indices));
 }
}
