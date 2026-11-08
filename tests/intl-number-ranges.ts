/// <reference lib="es2023.intl" />
export {};
for (const options of [{}, {style: 'currency', currency: 'USD'}, {style: 'currency', currency: 'USD', currencyDisplay: 'name', minimumFractionDigits: 0}, {style: 'currency', currency: 'USD', currencySign: 'accounting'}, {style: 'percent'}, {style: 'unit', unit: 'meter', unitDisplay: 'long'}, {notation: 'compact'}, {notation: 'compact', compactDisplay: 'long'}, {notation: 'scientific'}, {signDisplay: 'always'}, {numberingSystem: 'deva'}]) {
 const f = new Intl.NumberFormat('en-US', options as any);
 for (const start of [-Infinity, -2, -1, -0, 0, 1, 1.0001, 2, 1000, 1e6, Infinity]) for (const end of [-Infinity, -2, -1, -0, 0, 1, 1.0001, 2, 1000, 1e6, Infinity]) {
  console.log('range', JSON.stringify(options), String(start), String(end), f.formatRange(start, end), JSON.stringify(f.formatRangeToParts(start, end)));
 }
}

const formatter: any = new Intl.NumberFormat('en-US', {maximumFractionDigits: 2});
for (const method of ['formatRange', 'formatRangeToParts']) {
 console.log('metadata', method, formatter[method].name, formatter[method].length);
 for (const pair of [[undefined, 1], [1, undefined], [NaN, 1], [1, NaN], [Symbol('start'), 1], [1, Symbol('end')], [1n, 2n], ['9007199254740992.125', '9007199254740993.125']]) {
  try {console.log('endpoints', method, JSON.stringify(formatter[method](pair[0], pair[1])));}
  catch (error) {console.log('endpoint error', method, (error as Error).name);}
 }
 for (const fail of ['', 'start', 'end', 'undefined', 'nan']) {
  const trace: string[] = [];
  const marker = new Error('endpoint');
  const start = fail === 'undefined' ? undefined : {[Symbol.toPrimitive](hint: string) {trace.push('start ' + hint); if (fail === 'start') throw marker; return fail === 'nan' ? NaN : 1;}};
  const end = {[Symbol.toPrimitive](hint: string) {trace.push('end ' + hint); if (fail === 'end') throw marker; return 2;}};
  try {console.log('converted', method, JSON.stringify(formatter[method](start, end)));}
  catch (error) {console.log('conversion error', method, error === marker, (error as Error).name);}
  console.log('conversion order', trace.join(','));
 }
 let touched = false;
 try {formatter[method].call({}, {valueOf() {touched = true; return 1;}}, 2);}
 catch (error) {console.log('receiver first', method, (error as Error).name, touched);}
}
const first = formatter.formatRangeToParts(1, 2);
first[0].value = 'changed'; first[0].source = 'changed';
console.log('fresh range parts', JSON.stringify(formatter.formatRangeToParts(1, 2)));

for (const options of [
 {style: 'currency', currency: 'EUR'}, {style: 'currency', currency: 'USD', currencyDisplay: 'code'},
 {style: 'currency', currency: 'USD', currencySign: 'accounting', signDisplay: 'always'},
 {style: 'unit', unit: 'meter', unitDisplay: 'narrow'}, {style: 'unit', unit: 'kilometer-per-hour', unitDisplay: 'long', notation: 'compact', compactDisplay: 'long'},
 {style: 'percent', notation: 'compact'}, {style: 'currency', currency: 'USD', currencyDisplay: 'name', notation: 'compact', compactDisplay: 'long'}
]) {
 // Node ICU mislabels source spans after the currency-code nonbreaking space; compare its text only.
 const f: any = new Intl.NumberFormat('en-US', options as any);
 for (const start of [-2, -0, 0, 1, 2, 1000]) for (const end of [-2, -0, 0, 1, 2, 2000]) console.log('combined', JSON.stringify(options), String(start), String(end), f.formatRange(start, end), JSON.stringify((options as any).currencyDisplay === 'code' ? [] : f.formatRangeToParts(start, end)));
}

const typedParts: Intl.NumberRangeFormatPart[] = new Intl.NumberFormat('en-US').formatRangeToParts(1, 2);
for (const part of typedParts) console.log('typed part', part.type, part.value, part.source);
