console.log('trim', '[' + ' \t hello \r\n'.trim() + ']', '[' + '  hi  '.trimStart() + ']', '[' + '  hi  '.trimEnd() + ']');
const whitespace = '\u0009\u000a\u000b\u000c\u000d\u0020\u00a0\u1680\u2000\u2001\u2002\u2003\u2004\u2005\u2006\u2007\u2008\u2009\u200a\u2028\u2029\u202f\u205f\u3000\ufeff';
console.log('whitespace', whitespace.trim().length, (whitespace + 'x' + whitespace).trim());
for (const value of ['\u0085', '\u180e', '\u200b', '\u0000']) console.log('retained', value.trim().length, (' ' + value + ' ').trim().charCodeAt(0));
console.log('repeat', 'ab'.repeat(3), 'x'.repeat(2.9), 'x'.repeat(NaN), ''.repeat(1e100));
console.log('repeat-zero', 'x'.repeat(-0.9).length, 'x'.repeat(-0).length);
for (const count of [-1, Infinity, -Infinity]) {try {'x'.repeat(count);} catch(error) {console.log('repeat-range', count, error instanceof RangeError);}}
try {''.repeat(Infinity);} catch(error) {console.log('empty-infinity', error instanceof RangeError);}
console.log('padding', 'abc'.padStart(7, 'xy'), 'abc'.padEnd(7, 'xy'), 'abc'.padStart(5), 'abc'.padEnd(5));
console.log('no-padding', 'abc'.padStart(2), 'abc'.padEnd(NaN), 'abc'.padStart(Infinity, ''));
console.log('converted-filler', 'a'.padStart(5, null as any), 'a'.padEnd(3, undefined));
console.log('fraction', 'x'.padStart(3.9, '0'), 'x'.padEnd(-9, '0'));
console.log('surrogates', 'x'.padStart(2, '\uD83D\uDE00').charCodeAt(0), '\uD800'.repeat(2).charCodeAt(1));
const trim: any = ''.trim, repeat: any = ''.repeat, pad: any = ''.padStart;
console.log('generic', trim.call(12), repeat.call(true, 2), pad.call(12, 4, 0));
console.log('boxed', new String('  hi ').trim(), new String('ab').repeat(2), new String('a').padEnd(3, 'b'));
console.log('aliases', ''.trimLeft === ''.trimStart, ''.trimRight === ''.trimEnd, ''.trimLeft.name, ''.trimRight.name);
console.log('metadata', trim.length, repeat.length, pad.length);
let effects = '';
const receiver = {toString():string {effects += 'S';return 'a';}};
const count = {valueOf():number {effects += 'N';return 4;}};
const filler = {toString():string {effects += 'F';return 'xy';}};
console.log('order', pad.call(receiver, count, filler), effects);
effects = '';
console.log('unused-filler', pad.call('abcd', 2, filler), effects);
try {trim.call(null);} catch(error) {console.log('trim-null', error instanceof TypeError);}
try {repeat.call(undefined, count);} catch(error) {console.log('repeat-undefined', error instanceof TypeError);}
try {pad.call('a', Infinity, 'b');} catch(error) {console.log('pad-range', error instanceof RangeError);}
try {repeat.call('a', {valueOf():number {throw 'count failure';}});} catch(error) {console.log('count-throw', error);}
try {pad.call('a', 3, {toString():string {throw 'filler failure';}});} catch(error) {console.log('fill-throw', error);}
console.log('apply', repeat.apply('ab', [2]), pad.bind('x', 3)('0'));
const originalTrim = String.prototype.trim;
String.prototype.trim = () => 'overridden';
console.log('prototype-override', '  text  '.trim(), new String('text').trim());
String.prototype.trim = originalTrim;
console.log('prototype-restored', '  text  '.trim());
console.log('hidden', Object.keys(String.prototype).length);
console.log('long-repeat', 'ab'.repeat(1000).length, 'x'.padEnd(1001, 'ab').slice(997));
try {'ab'.repeat(1e100);} catch(error) {console.log('large-repeat', error instanceof RangeError);}
