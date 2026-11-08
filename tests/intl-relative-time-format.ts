export {};
for (const locale of ['en', 'en-US']) {
    for (const style of ['long', 'short', 'narrow']) {
        for (const numeric of ['always', 'auto']) {
            const formatter = new Intl.RelativeTimeFormat(locale, {style, numeric} as any);
            console.log('options', JSON.stringify(formatter.resolvedOptions()));
            for (const unit of ['year', 'quarter', 'month', 'week', 'day', 'hour', 'minute', 'second']) {
                for (const value of [-2, -1, -0, 0, 0.5, 1, 1.00001, 1.001, 1.25, 2, 1234.56789]) {
                    // ICU uses approximate matching for automatic near-integer labels.
                    if (numeric === 'auto' && (value === 1.00001 || value === 1.001)) continue;
                    console.log('relative', unit, String(value), formatter.format(value, unit as any), JSON.stringify(formatter.formatToParts(value, unit as any)));
                }
            }
        }
    }
}
for (const numberingSystem of ['deva', 'mathbold', 'bali']) {
    const formatter = new Intl.RelativeTimeFormat('en-US', {numberingSystem} as any);
    console.log('digits', numberingSystem, formatter.format(-1234.5, 'days'), JSON.stringify(formatter.formatToParts(-1234.5, 'day')));
}
const formatter = new Intl.RelativeTimeFormat('en-US');
console.log('plural unit', formatter.format(2, 'years'), formatter.format(2, 'quarters'));
console.log('metadata', Intl.RelativeTimeFormat.name, Intl.RelativeTimeFormat.length, formatter.format.name, formatter.format.length, formatter.formatToParts.name, formatter.formatToParts.length, formatter.resolvedOptions.length);
console.log('tag', Object.prototype.toString.call(formatter));
console.log('supported', Intl.RelativeTimeFormat.supportedLocalesOf(['en-US', 'zz', 'en-u-nu-deva', 'en-US']).join(','));
class Derived extends Intl.RelativeTimeFormat {field = 3; #secret = 4; read() {return this.#secret;}}
const child = new Derived('en-US');
console.log('derived', child.field, child.read(), child instanceof Derived, child instanceof Intl.RelativeTimeFormat, child.format(1, 'day'));
for (const method of [formatter.format, formatter.formatToParts, formatter.resolvedOptions]) {
    try {(method as any).call({}, 1, 'day');} catch (error) {console.log('receiver', (error as Error).name);}
}
for (const value of [NaN, Infinity, -Infinity, 1n, Symbol('value')]) {
    try {formatter.format(value as any, 'day');} catch (error) {console.log('value error', (error as Error).name);}
}
for (const unit of ['DAY', 'invalid', '', undefined, Symbol('unit')]) {
    try {formatter.format(1, unit as any);} catch (error) {console.log('unit error', (error as Error).name);}
}
for (const options of [null, {style: 'invalid'}, {numeric: 'invalid'}, {localeMatcher: 'invalid'}, {numberingSystem: 'bad_system'}]) {
    try {new Intl.RelativeTimeFormat('en-US', options as any);} catch (error) {console.log('option error', (error as Error).name);}
}
let trace = '';
console.log('coercion', formatter.format({valueOf() {trace += 'value|'; return -2;}} as any, {toString() {trace += 'unit|'; return 'days';}} as any), trace);
trace = '';
try {formatter.format(NaN, {toString() {trace += 'unit|'; throw new Error('unit');}} as any);} catch (error) {console.log('coercion error', (error as Error).name, trace);}
const options: any = {get localeMatcher() {trace += 'matcher|'; return 'lookup';}, get numberingSystem() {trace += 'digits|'; return 'latn';}, get style() {trace += 'style|'; return 'short';}, get numeric() {trace += 'numeric|'; return 'auto';}};
trace = ''; const configured = new Intl.RelativeTimeFormat('en-US', options);
console.log('option getters', trace, configured.format(-1, 'year'));
const first = formatter.formatToParts(2, 'day');
const second = formatter.formatToParts(2, 'day');
first[1].value = 'changed';
console.log('fresh', first !== second, first[1] !== second[1], second[1].value, Object.keys(second[1]).join(','));
