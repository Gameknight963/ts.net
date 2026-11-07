const format = new Intl.NumberFormat('en-US', {minimumFractionDigits: 2, maximumFractionDigits: 2});
for (const value of [1234.56, -1234.56, 0, -0, 1.005, 2.675, 0.125, -0.125, NaN, Infinity, -Infinity, 1e25, 5e-324]) console.log(format.format(value));
console.log(format.format === format.format, format.format.call(null, 123));
console.log(format.formatToParts(-1234.56).map(part => part.type + ':' + part.value).join('|'));
// CLR and Node use different French grouping spaces.
for (const locale of ['de-DE','fr-FR','en-IN']) console.log(locale, new Intl.NumberFormat(locale).format(1234567.89).replace(/[\u00a0\u202f]/g, " "));
for (const signDisplay of ['auto','always','never','exceptZero','negative'] as const) {
 const formatter = new Intl.NumberFormat('en', {signDisplay: signDisplay as any, maximumFractionDigits:0});
 console.log(signDisplay, [-1,-0.1,-0,0,0.1,1,NaN,Infinity,-Infinity].map(value => formatter.format(value)).join('|'));
}
const big = new Intl.NumberFormat('en', {minimumFractionDigits:2}); console.log(big.format(12345678901234567890n));
console.log(big.formatToParts(12345678901234567890n).map(part => part.value).join(''));
const options = format.resolvedOptions();
console.log(options.locale, options.style, options.minimumIntegerDigits, options.minimumFractionDigits, options.maximumFractionDigits, options.useGrouping, options.notation, options.signDisplay);
class Custom extends Intl.NumberFormat { extra = 3; }
const custom = new Custom('en'); console.log(custom instanceof Custom, custom instanceof Intl.NumberFormat, custom.extra, custom.format(123));
console.log(Intl.NumberFormat('en', {useGrouping:false}).format(1234));
console.log(Intl.NumberFormat.supportedLocalesOf(['en','de','zz-ZZ']).join('|'));
console.log(Object.prototype.toString.call(format));
let reads = 0; const mutable = {get minimumFractionDigits() {reads++; return 2;}};
const prepared = new Intl.NumberFormat('en', mutable); prepared.format(2); prepared.format(3); console.log(reads);
try { Object.getOwnPropertyDescriptor(Intl.NumberFormat.prototype, 'format')!.get!.call({}); } catch(error) { console.log('receiver', error instanceof TypeError); }
try { new Intl.NumberFormat('en', {minimumFractionDigits:4,maximumFractionDigits:2}); } catch(error) {console.log('digits', error instanceof RangeError);}

console.log(JSON.stringify(format.resolvedOptions()));
