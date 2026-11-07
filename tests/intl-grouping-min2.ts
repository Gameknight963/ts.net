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
