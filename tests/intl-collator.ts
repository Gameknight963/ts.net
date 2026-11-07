const collator = new Intl.Collator('en', { numeric: true, sensitivity: 'base' });
const words = ['file10', 'File2', 'file1']; words.sort(collator.compare); console.log(words.join('|'));
console.log(collator.compare === collator.compare, collator.compare('a', 'A'), collator.compare('a', 'b'), collator.compare('b', 'a'));
const compare = collator.compare; console.log(compare.call(null, '2', '10'));
for (const sensitivity of ['base', 'accent', 'case', 'variant'] as const) {
 const value = new Intl.Collator('en', {sensitivity});
 console.log(sensitivity, Math.sign(value.compare('a', 'A')), Math.sign(value.compare('a', '\u00e1')));
}
const punctuation = Intl.Collator('en', {ignorePunctuation: true}); console.log(punctuation.compare('a-b', 'ab'));
const options = collator.resolvedOptions();
console.log(options.locale, options.usage, options.sensitivity, options.numeric, options.ignorePunctuation, options.collation, options.caseFirst);
console.log(Intl.Collator.supportedLocalesOf(['en-US', 'de-DE', 'en-US', 'zz-ZZ']).join('|'));
console.log(Intl.Collator.supportedLocalesOf('fr').join('|'));
class CustomCollator extends Intl.Collator { extra = 3; }
const custom = new CustomCollator('en', {numeric:true}); console.log(custom instanceof CustomCollator, custom instanceof Intl.Collator, custom.extra, custom.compare('2','10'));
const alias = Intl; const constructor = alias.Collator; console.log(new constructor('en').compare('a','b'));
console.log(Object.prototype.toString.call(collator), Object.prototype.toString.call(Intl));
const descriptor = Object.getOwnPropertyDescriptor(Intl.Collator.prototype, 'compare')!;
try { descriptor.get!.call({}); } catch (error) { console.log('receiver', error instanceof TypeError); }
try { new Intl.Collator('en', { sensitivity: 'invalid' as any }); } catch (error) { console.log('sensitivity', error instanceof RangeError); }
try { new Intl.Collator('en', null as any); } catch (error) { console.log('null', error instanceof TypeError); }
try { new Intl.Collator('en', {localeMatcher:'invalid' as any}); } catch (error) { console.log('matcher', error instanceof RangeError); }
let reads = 0;
new Intl.Collator('en', {get sensitivity() {reads++; return 'base' as const;}}); console.log('reads', reads);

console.log(Intl.Collator.supportedLocalesOf([{toString() {return 'en-US';}} as any]).join('|'));
console.log(Intl.Collator.supportedLocalesOf([]).length);
try { Intl.Collator.supportedLocalesOf(['en', 3] as any); } catch (error) { console.log('locale entry', error instanceof TypeError); }
function shadow(Intl: any) { return Intl.Collator(); } console.log(shadow({Collator: () => 'local'}));
const inherited = Object.create({ numeric: true, sensitivity: 'base' });
console.log(new Intl.Collator('en', inherited).compare('A2','a10'));
console.log(new Intl.Collator('en-US', {usage:'search'}).resolvedOptions().usage);
for (const locale of ['de', 'sv', 'tr']) {
 const value = new Intl.Collator(locale, {sensitivity:'base'});
 console.log(locale, Math.sign(value.compare('z','\u00e4')), Math.sign(value.compare('I','\u0131')));
}
const mutable = {numeric:true}; const stable = new Intl.Collator('en', mutable); mutable.numeric = false;
const snapshot = stable.resolvedOptions(); snapshot.numeric = false;
console.log('snapshot', stable.compare('2','10'), stable.resolvedOptions().numeric);
