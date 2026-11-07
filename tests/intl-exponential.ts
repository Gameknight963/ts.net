for(const notation of ['scientific','engineering'] as const) {
 const options={notation};const formatter=new Intl.NumberFormat('en-US',options);
 for(const value of [0,-0,1,-1,12.3456,999.9999,9999.9999,0.0012345,1e25,5e-324,NaN,Infinity,-Infinity])console.log(notation,formatter.format(value),value.toLocaleString('en-US',options));
 for(const value of [0n,1n,-1n,999999n,123456789012345678901234567890n])console.log('big',formatter.format(value));
 console.log(formatter.formatToParts(-0.0012345).map(part=>part.type+':'+part.value).join('|'));
 console.log(JSON.stringify(formatter.resolvedOptions()));
 for(const options of [{maximumFractionDigits:0},{maximumSignificantDigits:2},{minimumIntegerDigits:5},{roundingPriority:'morePrecision',maximumSignificantDigits:2,maximumFractionDigits:1},{roundingIncrement:5,minimumFractionDigits:0,maximumFractionDigits:0}]) {
  const f=new Intl.NumberFormat('en-US',{notation,...options} as any);
  console.log([0.0009999,9.999,99.999,999.999,9999.999].map(value=>f.format(value)).join('|'));
 }
}
for(const style of ['percent','currency'])for(const notation of ['scientific','engineering']) {
 const f=new Intl.NumberFormat('en-US',{style,currency:'EUR',notation} as any);
 for(const value of [0.0012345,12345.67,123456789n])console.log(style,notation,f.format(value).replace(/[ \u00a0\u202f]/g,''));
 console.log(f.formatToParts(12345).filter(part=>part.type!=='literal'||part.value.trim()!=='').map(part=>part.type+':'+part.value).join('|'));
}
try{new Intl.NumberFormat('en',{notation:'wrong'} as any);}catch(error){console.log('invalid',error instanceof RangeError);}
