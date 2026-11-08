for(const locale of ['en-US','de-DE','en-IN']) {
 const formatter=new Intl.NumberFormat(locale,{useGrouping:'min2'} as any);
 for(const value of [0,999,1000,9999,10000,100000,1000000,-1234,1234.5,12345678901234567890n])console.log(locale,formatter.format(value));
 console.log((formatter.resolvedOptions() as any).useGrouping);
}
for(const minimumIntegerDigits of [1,4,5,7]) {
 const formatter=new Intl.NumberFormat('en-US',{useGrouping:'min2',minimumIntegerDigits} as any);
 for(const value of [0,12,1234,12345,12n])console.log(minimumIntegerDigits,formatter.format(value));
}
for(const style of ['percent','currency']) {
 const formatter=new Intl.NumberFormat('en-US',{style,currency:'USD',useGrouping:'min2',maximumFractionDigits:0} as any);
 for(const value of [12,1234,12345,1234n])console.log(style,formatter.format(value).replace(/[ \u00a0\u202f]/g,''));
}

for (const locale of ['es', 'es-ES']) {
 for (const useGrouping of ['auto', 'always', 'min2', false]) {
  for (const minimumIntegerDigits of [1, 5]) {
   const formatter = new Intl.NumberFormat(locale, {useGrouping, minimumIntegerDigits} as any);
   console.log('spanish options', JSON.stringify(formatter.resolvedOptions()));
   for (const value of [999, 1000, 9999, 10000, -1234.5, 1234n, 12345n, '1234.00000000000000001']) {
    console.log('spanish grouping', String(value), formatter.format(value as any), JSON.stringify(formatter.formatToParts(value as any)));
   }
  }
 }
}
