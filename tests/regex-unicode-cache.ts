export {};
const pattern = '^(?<letter>\\p{Letter})\\1$';
const first = new RegExp(pattern, 'dgu'), second = new RegExp(pattern, 'du');
console.log('fresh', first !== second, first.exec('aa')?.[0], first.lastIndex, second.lastIndex, second.exec('bb')?.groups?.letter);
for (const flags of ['u', 'su', 'mu', 'du', 'gu', 'uy']) {
 for (let repeat = 0; repeat < 2; repeat++) {
  const regex = new RegExp('^.$', flags);
  const match = regex.exec('\n');
  console.log('flags', flags, regex.flags, regex.unicode, JSON.stringify(match), JSON.stringify((match as any)?.indices), regex.lastIndex);
 }
}
let trace: string[] = [];
for (let repeat = 0; repeat < 2; repeat++) {
 const source = {toString() {trace.push('source'); return '\\p{Letter}+';}};
 const flags = {toString() {trace.push('flags'); return 'u';}};
 console.log('coerced', new RegExp(source as any, flags as any).test('abc'));
}
console.log('coercion order', trace.join(','));
const marker = new Error('flags');
try {new RegExp('\\p{Letter}+', {toString() {throw marker;}} as any);}
catch (error) {console.log('conversion exception', error === marker);}
for (let index = 0; index < 80; index++) {
 const regex = new RegExp('a{' + index + '}', 'u');
 if (!regex.test('a'.repeat(index))) throw new Error('Eviction changed matching');
}
first.lastIndex = 0;
console.log('after eviction', first.exec('aa')?.groups?.letter, new RegExp(pattern, 'du').exec('bb')?.groups?.letter);
class Derived extends RegExp {field = 7;}
for (let repeat = 0; repeat < 2; repeat++) {
 const regex = new Derived('\\p{Letter}+', 'u');
 console.log('subclass', regex instanceof Derived, regex.field, regex.test('abc'));
}
