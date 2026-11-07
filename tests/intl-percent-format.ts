// CLR and Node locale tables can use different spacing around percent signs.
function spacing(text: string): string { return text.replace(/[ \u00a0\u202f]/g, ''); }
const formatter = new Intl.NumberFormat('en-US', {style:'percent'});
for (const value of [0, -0, 0.125, -0.125, 0.145, -0.145, 0.001, 1.2345, 123.456, NaN, Infinity, -Infinity, 1e300, 5e-324]) console.log(spacing(formatter.format(value)));
for (const digits of [0, 1, 2, 5]) {
 const format = new Intl.NumberFormat('en', {style:'percent', minimumFractionDigits:digits, maximumFractionDigits:digits});
 for (const value of [0.145, 0.0045, -0.0045]) console.log(digits, spacing(format.format(value)), spacing(value.toLocaleString('en', {style:'percent',minimumFractionDigits:digits,maximumFractionDigits:digits})));
}
for (const locale of ['en-US','de-DE','tr-TR']) {
 const format = new Intl.NumberFormat(locale, {style:'percent',useGrouping:false,maximumFractionDigits:2});
 console.log(locale, spacing(format.format(-0.125)), spacing(format.format(0.125)));
}
for (const signDisplay of ['auto','always','never','exceptZero','negative'] as const) {
 const format = new Intl.NumberFormat('en', {style:'percent',signDisplay:signDisplay as any});
 console.log(signDisplay, [-0.0001,-0,0,0.125,-0.125,NaN,Infinity].map(value => spacing(format.format(value))).join('|'));
}
console.log(spacing(formatter.format(12345678901234567890n)));
console.log(spacing((12345678901234567890n).toLocaleString('en', {style:'percent'})));
const parts = new Intl.NumberFormat('en', {style:'percent',minimumFractionDigits:2}).formatToParts(-12.345);
console.log(parts.filter(part => part.type !== 'literal' || part.value.trim() !== '').map(part => part.type + ':' + part.value).join('|'));
console.log(JSON.stringify(formatter.resolvedOptions()));
const object = {valueOf() {return 0.125;}}; console.log(spacing(formatter.format(object as any)));
