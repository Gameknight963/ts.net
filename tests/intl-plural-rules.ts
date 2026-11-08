export {};
const options: any[] = [{}, {minimumIntegerDigits: 3}, {minimumFractionDigits: 1}, {maximumFractionDigits: 0}, {minimumSignificantDigits: 2, maximumSignificantDigits: 3}, {maximumSignificantDigits: 1}, {roundingMode: 'ceil', maximumFractionDigits: 0}, {roundingMode: 'floor', maximumFractionDigits: 0}, {roundingMode: 'halfEven', maximumFractionDigits: 0}, {roundingIncrement: 2, minimumFractionDigits: 0, maximumFractionDigits: 0}, {minimumFractionDigits: 1, trailingZeroDisplay: 'stripIfInteger'}];
for (const locale of ['en', 'en-US', 'en-GB']) {
    for (const type of ['cardinal', 'ordinal']) {
        for (const digits of options) {
            const rules = new Intl.PluralRules(locale, {type, ...digits} as any);
            console.log('options', JSON.stringify(rules.resolvedOptions()));
            for (const value of [-23, -13, -3, -2, -1.1, -1, -0.1, -0, 0, 0.1, 0.999, 1, 1.00001, 1.1, 1.5, 2, 2.5, 3, 4, 11, 12, 13, 21, 22, 23, 101, 111, 121, NaN, Infinity, -Infinity]) console.log('select', value, rules.select(value));
        }
    }
}
const rules = new Intl.PluralRules('en-US');
const range: any = (rules as any).selectRange;
for (const [first, last] of [[0, 2], [1, 2], [2, 3], [3, 1], [-1, 2], [Infinity, 1]]) console.log('range', first, last, range.call(rules, first, last));
for (const [first, last] of [[undefined, 1], [1, undefined], [NaN, 1], [1, NaN], [1n, 2], [1, Symbol('end')]]) {
    try {range.call(rules, first, last);} catch (error) {console.log('range error', (error as Error).name);}
}
console.log('metadata', Intl.PluralRules.name, Intl.PluralRules.length, rules.select.name, rules.select.length, range.name, range.length, rules.resolvedOptions.length);
console.log('tag', Object.prototype.toString.call(rules));
console.log('supported', Intl.PluralRules.supportedLocalesOf(['en-US', 'zz', 'en-GB-u-nu-deva', 'en-US']).join(','));
class Derived extends Intl.PluralRules {field = 7; #secret = 8; read() {return this.#secret;}}
const child = new Derived('en-US');
console.log('derived', child.field, child.read(), child instanceof Derived, child instanceof Intl.PluralRules, child.select(1));
for (const method of [rules.select, range, rules.resolvedOptions]) {
    try {(method as any).call({}, 1, 2);} catch (error) {console.log('receiver', (error as Error).name);}
}
for (const value of [1n, Symbol('value')]) try {rules.select(value as any);} catch (error) {console.log('select error', (error as Error).name);}
for (const option of [null, {type: 'invalid'}, {minimumFractionDigits: -1}, {roundingMode: 'invalid'}, {maximumSignificantDigits: 0}]) {
    try {new Intl.PluralRules('en-US', option as any);} catch (error) {console.log('option error', (error as Error).name);}
}
let trace = '';
console.log('coercion', rules.select({valueOf() {trace += 'number|'; return 1;}} as any), trace);
trace = '';
console.log('range coercion', range.call(rules, {valueOf() {trace += 'first|'; return 1;}}, {valueOf() {trace += 'last|'; return 2;}}), trace);
const getterOptions: any = {};
for (const key of ['localeMatcher', 'type', 'minimumIntegerDigits', 'minimumFractionDigits', 'maximumFractionDigits', 'minimumSignificantDigits', 'maximumSignificantDigits', 'roundingIncrement', 'roundingMode', 'roundingPriority', 'trailingZeroDisplay']) Object.defineProperty(getterOptions, key, {get() {trace += key + '|'; return undefined;}});
Object.defineProperty(getterOptions, 'style', {get() {throw new Error('irrelevant style');}});
trace = ''; const configured = new Intl.PluralRules('en-US', getterOptions); console.log('getters', trace, configured.select(1));
const first = rules.resolvedOptions(), second = rules.resolvedOptions();
first.pluralCategories[0] = 'changed' as any;
console.log('fresh options', first !== second, first.pluralCategories !== second.pluralCategories, second.pluralCategories.join(','));
