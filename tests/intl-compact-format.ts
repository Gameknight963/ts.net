export {};
for (const style of ['decimal', 'percent', 'currency', 'unit']) for (const compactDisplay of ['short', 'long']) {
 for (const precision of [{}, {maximumFractionDigits: 1}, {maximumSignificantDigits: 3}, {minimumFractionDigits: 2}, {useGrouping: 'always'}]) {
  const f = new Intl.NumberFormat('en-US', {style, currency: 'USD', unit: 'meter', notation: 'compact', compactDisplay, ...precision} as any);
  console.log('options', JSON.stringify(f.resolvedOptions()));
  for (const value of [-1234567, -1, -0, 0, 0.001, 0.9999, 1, 12, 99, 999, 999.5, 1000, 1234, 9999, 12345, 999499, 999500, 1e6, 1e9, 1e12, 1e15, NaN, Infinity, -Infinity, 1234n, 123456789012345678901n]) console.log(style, compactDisplay, String(value), f.format(value), JSON.stringify(f.formatToParts(value)));
 }
}

for (const options of [{minimumIntegerDigits: 4}, {roundingMode: 'floor'}, {roundingMode: 'ceil'}, {minimumFractionDigits: 2, trailingZeroDisplay: 'stripIfInteger'}, {numberingSystem: 'mathbold'}, {signDisplay: 'exceptZero'}, {useGrouping: false}]) {
 const f = new Intl.NumberFormat('en-US', {notation: 'compact', ...options} as any);
 console.log('extra options', JSON.stringify(f.resolvedOptions()));
 for (const value of [-999.99, -0.0001, -0, 0, 1, 999, 999.99, 1000, 999999.99, '12345678901234567890.1234']) console.log('extra', String(value), f.format(value as any), JSON.stringify(f.formatToParts(value as any)));
}
for (const compactDisplay of ['short', 'long']) for (const style of ['unit', 'currency']) {
 const f = new Intl.NumberFormat('en-US', {notation: 'compact', compactDisplay, style, currency: 'USD', currencyDisplay: 'name', unit: 'meter', unitDisplay: 'long'} as any);
 for (const value of [1, 1000, 1200, 1e6]) console.log('named', style, compactDisplay, f.format(value), JSON.stringify(f.formatToParts(value)));
}
