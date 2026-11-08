export {};
const keys = ['notation', 'minimumIntegerDigits', 'minimumFractionDigits', 'maximumFractionDigits', 'minimumSignificantDigits', 'maximumSignificantDigits', 'roundingIncrement', 'roundingMode', 'roundingPriority', 'trailingZeroDisplay', 'compactDisplay', 'useGrouping', 'signDisplay'];
let trace: string[] = [];
const options: any = {};
for (const key of keys) Object.defineProperty(options, key, {get() {trace.push(key); return undefined;}});
console.log('format', new Intl.NumberFormat('en-US', options).format(1234.5));
console.log('order', trace.join(','));
for (const compactDisplay of ['short', 'long', 'invalid', null]) {
    try {console.log('compact option', compactDisplay, new Intl.NumberFormat('en-US', {compactDisplay} as any).format(12));}
    catch (error) {console.log('compact error', compactDisplay, (error as Error).name);}
}
for (const roundingIncrement of [2, 5, 25]) {
    try {
        const formatter = new Intl.NumberFormat('en-US', {roundingIncrement} as any);
        console.log('increment', roundingIncrement, formatter.format(123.6), formatter.resolvedOptions().minimumFractionDigits, formatter.resolvedOptions().maximumFractionDigits);
    } catch (error) {console.log('increment error', roundingIncrement, (error as Error).name);}
}
trace = [];
const delayed: any = {
    get minimumFractionDigits() {trace.push('get minimum'); return {valueOf() {trace.push('convert minimum'); return 1;}};},
    get maximumFractionDigits() {trace.push('get maximum'); return 2;},
    get roundingMode() {trace.push('rounding mode'); return 'halfExpand';},
    get roundingPriority() {trace.push('priority'); return 'auto';},
    get compactDisplay() {trace.push('compact display'); return 'long';},
    get useGrouping() {trace.push('grouping'); return false;},
    get signDisplay() {trace.push('sign'); return 'always';}
};
console.log('delayed', new Intl.NumberFormat('en-US', delayed).format(1.25), trace.join(','));

console.log('locale methods', (1234.5).toLocaleString('en-US', {compactDisplay: 'long'}), (1234n).toLocaleString('en-US', {compactDisplay: 'short'}));
for (const failed of ['minimumIntegerDigits', 'roundingMode', 'compactDisplay', 'useGrouping', 'signDisplay']) {
    trace = [];
    const failing: any = {};
    for (const key of keys) Object.defineProperty(failing, key, {get() {
        trace.push(key);
        if (key === failed) throw new Error('stop');
        return undefined;
    }});
    try {new Intl.NumberFormat('en-US', failing);}
    catch (error) {console.log('stopped', failed, (error as Error).message, trace.join(','));}
}

for (const style of ['decimal', 'percent', 'currency']) {
 for (const unit of ['meter', 'mile-per-hour', 'fluid-ounce-per-acre', 'meter-per-second-per-hour', 'Meter', '', null, Symbol('unit')]) {
  try {console.log('ignored unit', style, typeof unit, new Intl.NumberFormat('en-US', {style, currency: 'USD', unit} as any).format(12));}
  catch (error) {console.log('unit error', style, typeof unit, (error as Error).name);}
 }
 for (const unitDisplay of ['short', 'long', 'narrow', 'invalid', null, Symbol('display')]) {
  try {console.log('ignored display', style, typeof unitDisplay, new Intl.NumberFormat('en-US', {style, currency: 'USD', unitDisplay} as any).format(12));}
  catch (error) {console.log('display error', style, typeof unitDisplay, (error as Error).name);}
 }
}
for (const invalid of [false, true]) {
 trace = [];
 const unitOptions: any = {
  get currencyDisplay() {trace.push('currencyDisplay'); return 'symbol';},
  get currencySign() {trace.push('currencySign'); return 'standard';},
  get unit() {trace.push('get unit'); return {[Symbol.toPrimitive](hint: string) {trace.push(hint); return invalid ? 'invalid' : 'meter';}};},
  get unitDisplay() {trace.push('get unitDisplay'); return {toString() {trace.push('convert display'); return 'long';}};},
  get notation() {trace.push('notation'); return 'standard';}
 };
 try {console.log('unit coercion', new Intl.NumberFormat('en-US', unitOptions).format(12));}
 catch (error) {console.log('unit coercion error', (error as Error).name);}
 console.log('unit option order', trace.join(','));
}
