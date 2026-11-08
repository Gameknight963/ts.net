export {};
const units = ["acre", "bit", "byte", "celsius", "centimeter", "day", "degree", "fahrenheit", "fluid-ounce", "foot", "gallon", "gigabit", "gigabyte", "gram", "hectare", "hour", "inch", "kilobit", "kilobyte", "kilogram", "kilometer", "liter", "megabit", "megabyte", "meter", "microsecond", "mile", "mile-scandinavian", "milliliter", "millimeter", "millisecond", "minute", "month", "nanosecond", "ounce", "percent", "petabyte", "pound", "second", "stone", "terabit", "terabyte", "week", "yard", "year", "kilometer-per-hour", "mile-per-hour", "meter-per-second", "liter-per-kilometer"];
for (const unit of units) for (const unitDisplay of ['long', 'short', 'narrow']) {
 const f = new Intl.NumberFormat('en-US', {style: 'unit', unit, unitDisplay} as any);
 console.log('options', JSON.stringify(f.resolvedOptions()));
 for (const value of [-2, -1, -0, 0, 0.5, 1, 1.5, 2, 1234.5, NaN, Infinity, -Infinity, 1n, 2n]) console.log(unit, unitDisplay, f.format(value), JSON.stringify(f.formatToParts(value)));
}

for (const options of [
 {minimumFractionDigits: 1}, {minimumIntegerDigits: 3}, {maximumFractionDigits: 0},
 {trailingZeroDisplay: 'stripIfInteger', minimumFractionDigits: 2}, {notation: 'scientific'},
 {signDisplay: 'always'}, {numberingSystem: 'deva'}, {maximumSignificantDigits: 1}
]) {
 const f = new Intl.NumberFormat('en-US', {style: 'unit', unit: 'meter', unitDisplay: 'long', ...options} as any);
 for (const value of [-1, -0, 0, 0.9999, 1, 1.01, 1000, 1n, '1.00000000000000001']) console.log('rounding', JSON.stringify(options), f.format(value as any), JSON.stringify(f.formatToParts(value as any)));
}
console.log('locale methods', (1).toLocaleString('en-US', {style: 'unit', unit: 'meter'} as any), (2n).toLocaleString('en-US', {style: 'unit', unit: 'meter', unitDisplay: 'long'} as any));

for (const options of [{}, {unit: 'invalid'}, {unit: 'meter-per-second-per-hour'}, {unit: Symbol('unit')}, {unit: 'meter', unitDisplay: 'invalid'}]) {
 try {new Intl.NumberFormat('en-US', {style: 'unit', ...options} as any);}
 catch (error) {console.log('invalid unit', (error as Error).name);}
}
let conversions = 0;
const converted = new Intl.NumberFormat('en-US', {style: 'unit', unit: 'meter'} as any);
console.log('conversion once', converted.formatToParts({[Symbol.toPrimitive](hint: string) {conversions++; console.log('hint', hint); return 2;}} as any).map(part => part.value).join(''), conversions);
