// CLR and Node currency tables may use different spacing.
function spacing(value: string): string { return value.replace(/[ \u00a0\u202f]/g, ''); }
for (const pair of [['en-US','USD'],['de-DE','EUR'],['en-GB','GBP'],['ja-JP','JPY'],['en-US','EUR']]) {
 const formatter = new Intl.NumberFormat(pair[0], {style:'currency',currency:pair[1]});
 for (const value of [0,-0,1.005,-1.005,1234.56,-1234.56,NaN,Infinity,-Infinity]) console.log(pair.join('|'),spacing(formatter.format(value)));
 console.log(formatter.resolvedOptions().minimumFractionDigits,formatter.resolvedOptions().maximumFractionDigits);
}
for (const currencyDisplay of ['symbol','narrowSymbol','code'] as const) {
 const formatter = new Intl.NumberFormat('en-US', {style:'currency',currency:'usd',currencyDisplay});
 console.log(currencyDisplay,spacing(formatter.format(1234.56)),spacing((1234.56).toLocaleString('en-US',{style:'currency',currency:'USD',currencyDisplay})));
}
for (const signDisplay of ['auto','always','never','exceptZero','negative'] as const) {
 const formatter = new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',signDisplay:signDisplay as any});
 console.log(signDisplay, [-1,-0,0,1,NaN].map(value => spacing(formatter.format(value))).join('|'));
}
const rounded = new Intl.NumberFormat('en',{style:'currency',currency:'USD',maximumFractionDigits:0}); console.log(spacing(rounded.format(12.5)));
const big = new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}); console.log(spacing(big.format(12345678901234567890n)));
console.log(big.formatToParts(-1234.56).map(part=>part.type+':'+part.value).join('|'));
console.log(JSON.stringify(big.resolvedOptions()));
const unknown = new Intl.NumberFormat('en',{style:'currency',currency:'XYZ'});console.log(spacing(unknown.format(3)));
for (const currency of ['US','USDD','u$d','u\u017fd'])try{new Intl.NumberFormat('en',{style:'currency',currency});}catch(error){console.log('code',error instanceof RangeError);}
try{new Intl.NumberFormat('en',{style:'currency'});}catch(error){console.log('missing',error instanceof TypeError);}
let reads=0;const formatter=new Intl.NumberFormat('en',{style:'currency',get currency(){reads++;return 'USD';}});formatter.format(1);formatter.format(2);console.log('reads',reads);
for (const currency of ['BHD','KRW']) {
 const value = new Intl.NumberFormat('en-US',{style:'currency',currency});
 console.log(currency,spacing(value.format(1.2345)),value.resolvedOptions().minimumFractionDigits,value.resolvedOptions().maximumFractionDigits);
}
const partsCode = new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',currencyDisplay:'code'}).formatToParts(3);
console.log(partsCode.filter(part=>part.type!=='literal'||part.value.trim()!=='').map(part=>part.type+':'+part.value).join('|'));
