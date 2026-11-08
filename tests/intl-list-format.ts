export {};
for (const locale of ['en', 'en-US', 'en-GB']) {
    for (const type of ['conjunction', 'disjunction', 'unit']) {
        for (const style of ['long', 'short', 'narrow']) {
            const formatter = new Intl.ListFormat(locale, {type, style} as any);
            console.log('options', JSON.stringify(formatter.resolvedOptions()));
            for (const values of [[], ['A'], ['A', 'B'], ['A', 'B', 'C'], ['A', 'B', 'C', 'D'], ['', 'B', ''], ['', '', ''], ['A, B', 'C and D']]) {
                console.log('list', JSON.stringify(values), formatter.format(values), JSON.stringify(formatter.formatToParts(values)));
            }
        }
    }
}
const formatter = new Intl.ListFormat('en-US');
console.log('empty', (formatter.format as any)(), JSON.stringify((formatter.formatToParts as any)()));
console.log('iterable', formatter.format('a\ud83d\ude00b'), formatter.format(new Set(['first', 'second'])), formatter.format((function*() {yield 'first'; yield 'second';})()));
console.log('metadata', Intl.ListFormat.name, Intl.ListFormat.length, formatter.format.name, formatter.format.length, formatter.formatToParts.name, formatter.formatToParts.length, formatter.resolvedOptions.name, formatter.resolvedOptions.length);
console.log('tag', Object.prototype.toString.call(formatter));
console.log('supported', Intl.ListFormat.supportedLocalesOf(['en-US', 'zz', 'en-GB-u-hc-h12', 'en-US']).join(','));
class Derived extends Intl.ListFormat {field = 7; #secret = 8; read() {return this.#secret;}}
const child = new Derived('en-US');
console.log('derived', child.field, child.read(), child instanceof Derived, child instanceof Intl.ListFormat, child.format(['A', 'B']));
for (const method of [formatter.format, formatter.formatToParts, formatter.resolvedOptions]) {
    try {(method as any).call({}, ['A']);} catch (error) {console.log('receiver', (error as Error).name);}
}
for (const input of [null, 1, {}, ['A', 1], ['A', new String('B')], ['A', undefined], ['A', Symbol('B')]]) {
    try {formatter.format(input as any);} catch (error) {console.log('input error', (error as Error).name);}
}
for (const options of [null, {type: 'invalid'}, {style: 'invalid'}, {localeMatcher: 'invalid'}]) {
    try {new Intl.ListFormat('en-US', options as any);} catch (error) {console.log('options error', (error as Error).name);}
}
let trace = '';
const options: any = {get localeMatcher() {trace += 'matcher|'; return 'lookup';}, get type() {trace += 'type|'; return 'unit';}, get style() {trace += 'style|'; return 'narrow';}};
console.log('getters', new Intl.ListFormat('en-US', options).format(['A', 'B']), trace);
for (const throwing of [false, true]) {
    trace = '';
    const input: any = {[Symbol.iterator]() {trace += 'open|'; let index = 0; return {next() {trace += 'next|'; return {value: index++ ? 1 : 'A', done: false};}, return() {trace += 'return|'; if (throwing) throw new Error('close'); return {};}};}};
    try {formatter.format(input);} catch (error) {console.log('close', throwing, (error as Error).name, trace);}
}

console.log('locale fallback', new Intl.ListFormat(['zz', 'en-GB']).resolvedOptions().locale, new Intl.ListFormat('en-US-u-hc-h12').resolvedOptions().locale);
try {(Intl.ListFormat as any)();} catch (error) {console.log('requires new', (error as Error).name);}
for (const phase of ['next', 'done', 'value']) {
    trace = '';
    const input: any = {[Symbol.iterator]() {return {
        next() {trace += 'next|'; if (phase === 'next') throw new Error('step'); return {
            get done() {trace += 'done|'; if (phase === 'done') throw new Error('done'); return false;},
            get value() {trace += 'value|'; throw new Error('value');}
        };},
        return() {trace += 'return|'; return {};}
    };}};
    try {formatter.format(input);} catch (error) {console.log('step error', phase, (error as Error).message, trace);}
}
trace = '';
try {formatter.format([{toString() {trace += 'coerce'; return 'A';}}] as any);} catch (error) {console.log('no coercion', (error as Error).name, trace);}
const first = formatter.formatToParts(['A', 'B']);
const second = formatter.formatToParts(['A', 'B']);
first[0].value = 'changed';
console.log('fresh parts', first !== second, first[0] !== second[0], second[0].value, Object.keys(second[0]).join(','));
