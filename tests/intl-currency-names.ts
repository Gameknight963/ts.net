export {};
for (const currency of ["AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AUD", "AWG", "AZN", "BAM", "BBD", "BDT", "BGN", "BHD", "BIF", "BMD", "BND", "BOB", "BRL", "BSD", "BTN", "BWP", "BYN", "BZD", "CAD", "CDF", "CHF", "CLP", "CNY", "COP", "CRC", "CUC", "CUP", "CVE", "CZK", "DJF", "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "EUR", "FJD", "FKP", "GBP", "GEL", "GHS", "GIP", "GMD", "GNF", "GTQ", "GYD", "HKD", "HNL", "HRK", "HTG", "HUF", "IDR", "ILS", "INR", "IQD", "IRR", "ISK", "JMD", "JOD", "JPY", "KES", "KGS", "KHR", "KMF", "KPW", "KRW", "KWD", "KYD", "KZT", "LAK", "LBP", "LKR", "LRD", "LSL", "LYD", "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR", "MWK", "MXN", "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "OMR", "PAB", "PEN", "PGK", "PHP", "PKR", "PLN", "PYG", "QAR", "RON", "RSD", "RUB", "RWF", "SAR", "SBD", "SCR", "SDG", "SEK", "SGD", "SHP", "SLE", "SLL", "SOS", "SRD", "SSP", "STN", "SVC", "SYP", "SZL", "THB", "TJS", "TMT", "TND", "TOP", "TRY", "TTD", "TWD", "TZS", "UAH", "UGX", "USD", "UYU", "UZS", "VES", "VND", "VUV", "WST", "XAF", "XCD", "XCG", "XDR", "XOF", "XPF", "XSU", "YER", "ZAR", "ZMW", "ZWG", "ZWL", "XXX", "XTS", "ZZZ"]) {
 for (const minimumFractionDigits of [0, 2]) {
  const f = new Intl.NumberFormat('en-US', {style: 'currency', currency, currencyDisplay: 'name', minimumFractionDigits, maximumFractionDigits: 2});
  console.log('options', JSON.stringify(f.resolvedOptions()));
  for (const value of [-2, -1, -0, 0, 0.5, 1, 1.5, 2, 1234.5, NaN, Infinity, -Infinity, 1n, 2n]) console.log(currency, f.format(value), JSON.stringify(f.formatToParts(value)));
 }
}
for (const currencySign of ['standard', 'accounting']) for (const signDisplay of ['auto', 'always', 'never', 'exceptZero', 'negative']) {
 const f = new Intl.NumberFormat('en-US', {style: 'currency', currency: 'USD', currencyDisplay: 'name', currencySign, signDisplay} as any);
 for (const value of [-1, -0, 0, 1, NaN, Infinity]) console.log('sign', currencySign, signDisplay, f.format(value), JSON.stringify(f.formatToParts(value)));
}

for (const currency of ['USD', 'EUR', 'JPY', 'GBP', 'INR', 'CHF', 'ZZZ']) {
 const f = new Intl.NumberFormat('en-US', {style: 'currency', currency, currencyDisplay: 'name'});
 for (const value of [0, 1, 1.5, 2]) console.log('default digits', currency, f.format(value), JSON.stringify(f.resolvedOptions()));
}
for (const options of [{minimumIntegerDigits: 3}, {minimumFractionDigits: 2, trailingZeroDisplay: 'stripIfInteger'}, {notation: 'scientific'}, {numberingSystem: 'deva'}]) {
 const f = new Intl.NumberFormat('en-US', {style: 'currency', currency: 'USD', currencyDisplay: 'name', minimumFractionDigits: 0, ...options} as any);
 for (const value of [-1, 0, 0.9999, 1, 1.01, 1000, 1n]) console.log('rounding', JSON.stringify(options), f.format(value), JSON.stringify(f.formatToParts(value)));
}
console.log('locale names', (1).toLocaleString('en-US', {style: 'currency', currency: 'USD', currencyDisplay: 'name'}), (2n).toLocaleString('en-US', {style: 'currency', currency: 'EUR', currencyDisplay: 'name'}));
