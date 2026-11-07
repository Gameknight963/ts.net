// Native currency-code spacing is retained, including for NaN and infinity.
for(const currencyDisplay of ['symbol','code','narrowSymbol'] as const) {
 for(const signDisplay of ['auto','always','never','exceptZero','negative'] as const) {
  const formatter=new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',currencySign:'accounting',currencyDisplay,signDisplay:signDisplay as any});
  console.log('options',currencyDisplay,signDisplay);
  for(const value of [-1234.56,-0.001,-0,0,0.001,1234.56,NaN,Infinity,-Infinity]) {
   const text=formatter.format(value);console.log(text.replace(/\u00a0/g,''),formatter.formatToParts(value).map(part=>part.value).join('')===text);
  }
 }
}
console.log('bigint',new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',currencySign:'accounting'}).format(-1234567890123456789n));
console.log('number-method',(-1234.56).toLocaleString('en-US',{style:'currency',currency:'USD',currencySign:'accounting'}));
console.log('resolved',new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',currencySign:'accounting'}).resolvedOptions().currencySign);
