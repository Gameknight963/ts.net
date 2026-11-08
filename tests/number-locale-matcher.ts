export {};
for (const localeMatcher of ['lookup', 'best fit', undefined] as const) {
    const options = {localeMatcher};
    console.log('valid', localeMatcher, new Intl.NumberFormat('en-US', options).format(1234.5));
    console.log('number', (1234.5).toLocaleString('en-US', options));
    console.log('bigint', (1234n).toLocaleString('en-US', options));
}
for (const localeMatcher of ['', 'LOOKUP', 'best-fit', null, 1, Symbol('matcher')]) {
    let reads = 0;
    const options = {localeMatcher, get numberingSystem() {reads++; return 'latn';}} as any;
    try {new Intl.NumberFormat('en-US', options);}
    catch (error) {console.log('invalid', typeof localeMatcher, (error as Error).name, reads);}
}
let log = '';
const options = {
    get localeMatcher() {log += 'M'; return {toString() {log += 'C'; return 'lookup';}};},
    get numberingSystem() {log += 'N'; return 'deva';},
    get style() {log += 'S'; return 'decimal';}
};
console.log('order', new Intl.NumberFormat('en-US', options as any).format(1234), log);
let touched = false;
try {new Intl.NumberFormat('bad_locale', {get localeMatcher() {touched = true; return 'lookup' as const;}});}
catch (error) {console.log('locale first', (error as Error).name, touched);}
