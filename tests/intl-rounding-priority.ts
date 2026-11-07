for(const roundingPriority of ['morePrecision','lessPrecision']) {
 for(const digits of [2,3,5]) {
  const options:any={roundingPriority,maximumFractionDigits:2,maximumSignificantDigits:digits};
  const formatter=new Intl.NumberFormat('en-US',options);
  for(const value of [0,-0,0.00012345,0.009999,0.09999,0.9999,1.2345,9.999,99.999,12345.6789,-9.999,1e25,5e-324])console.log(roundingPriority,digits,formatter.format(value),value.toLocaleString('en-US',options));
  for(const value of [0n,1n,12345n,123456789012345678901n])console.log('big',formatter.format(value));
  console.log(JSON.stringify(formatter.resolvedOptions()));
 }
 const defaults=new Intl.NumberFormat('en-US',{roundingPriority} as any);console.log(JSON.stringify(defaults.resolvedOptions()));
 const padded=new Intl.NumberFormat('en-US',{roundingPriority,minimumFractionDigits:2,minimumSignificantDigits:2} as any);console.log('minimum',padded.format(1));
}
for(const style of ['percent','currency'])for(const roundingPriority of ['morePrecision','lessPrecision']) {
 const formatter=new Intl.NumberFormat('en-US',{style,currency:'USD',roundingPriority,maximumFractionDigits:2,maximumSignificantDigits:3} as any);
 for(const value of [0.12345,9.999,-123.456,123456789n])console.log(style,roundingPriority,formatter.format(value).replace(/[ \u00a0\u202f]/g,''));
}
try{new Intl.NumberFormat('en',{roundingPriority:'wrong'} as any);}catch(error){console.log('priority',error instanceof RangeError);}
try{new Intl.NumberFormat('en',{roundingPriority:'morePrecision',roundingIncrement:5} as any);}catch(error){console.log('increment',error instanceof TypeError);}
let reads=0;const options:any={get roundingPriority(){reads++;return 'morePrecision';}};const formatter=new Intl.NumberFormat('en',options);formatter.format(1);formatter.format(2);console.log(reads);
