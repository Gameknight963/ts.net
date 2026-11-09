export {};
for(const locale of ['en-US','de-DE','fr-FR','ja-JP'])for(const timeZone of ['UTC','+05:30','America/New_York'])for(const options of [
 {year:'numeric',month:'short',day:'numeric'},
 {year:'numeric',month:'numeric',day:'numeric',hour:'2-digit',minute:'2-digit',hourCycle:'h23'},
 {weekday:'long',year:'numeric',month:'long',day:'numeric'},
 {dateStyle:'full',timeStyle:'short'},
 {hour:'2-digit',minute:'2-digit',hourCycle:'h23'}
] as Intl.DateTimeFormatOptions[]) {
 const f=new Intl.DateTimeFormat(locale,{timeZone,...options});
 for(const pair of [[Date.UTC(9999,11,31,20),Date.UTC(10000,0,1,3)],[Date.UTC(10000,0,3,9),Date.UTC(10000,0,4,10)],[Date.UTC(100000,0,3,9),Date.UTC(100000,1,4,10)],[8640000000000000-86400000,8640000000000000],[8640000000000000-1,8640000000000000]]) {
  const text=f.formatRange(pair[0],pair[1]);// CLDR versions differ in date/time connectors and Japanese month widths.
  if(locale==='ja-JP' && (options.month==='long' || options.month==='short')){console.log(locale,timeZone,JSON.stringify(options),pair.join(','),text.length>0 && text===f.formatRange(pair[0],pair[1]));continue;}
  console.log(locale,timeZone,JSON.stringify(options),pair.join(','),text.replace(/\s/g,'').replace(/(?:at|um|\u00e0)(?=\d)/g,'').replace(/,/g,''));
 }
}
const f=new Intl.DateTimeFormat('en-US',{timeZone:'UTC',year:'numeric',month:'short',day:'numeric'}),first=Date.UTC(10000,0,3,9);
console.log('equal',f.formatRange(first,first)===f.format(first),f.formatRange(first,first+1)===f.format(first));

for(const locale of ['en-US','de-DE','ja-JP'])for(const timeZone of ['UTC','+05:30','America/New_York']) {
 const f=new Intl.DateTimeFormat(locale,{timeZone,year:'numeric',month:'long',day:'numeric'}),start=Date.UTC(10000,0,3,9);
 console.log('one day',locale,timeZone,f.formatRange(start,start+1)===f.format(start));
}
