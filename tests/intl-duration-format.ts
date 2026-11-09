export {};
const values=[{seconds:0},{hours:1},{minutes:2},{seconds:3,milliseconds:4},{hours:1,seconds:1},{years:1,months:2,weeks:3,days:4,hours:5,minutes:6,seconds:7,milliseconds:8,microseconds:9,nanoseconds:10},{hours:-1,minutes:-2},{years:-1,months:-2},{seconds:-1,milliseconds:-999},{milliseconds:999},{years:1,minutes:2}];
for(const locale of ['en-US','en-GB','de-DE','fr-FR','ru-RU','ja-JP'])for(const options of [
 {style:'long'},{style:'short'},{style:'narrow'},{style:'digital'},
 {hours:'numeric'},{minutes:'numeric'},{seconds:'numeric'},{milliseconds:'numeric'},{microseconds:'numeric'},{nanoseconds:'numeric'},
 {style:'digital',fractionalDigits:2},{style:'digital',fractionalDigits:9},{hoursDisplay:'always'},
 {style:'digital',hoursDisplay:'auto',minutesDisplay:'auto',secondsDisplay:'auto'},
 {style:'digital',minutesDisplay:'auto'}, {style:'digital',numberingSystem:'deva'}
] as Intl.DurationFormatOptions[]) {
 const f=new Intl.DurationFormat(locale,options);console.log(locale,JSON.stringify(options),JSON.stringify(f.resolvedOptions()));
 for(const value of values) {
  const text=f.format(value),parts=f.formatToParts(value);
  if(parts.map(part=>part.value).join('')!==text)throw new Error('Duration parts differ from text');
  const normalized=parts.map(part=>({...part,value:part.type==='unit' && locale==='ru-RU' ? part.value.replace(/\.$/,'').replace(/^\u0441\u0435\u043a$/,'\u0441') : part.type==='unit' && locale==='de-DE' && options.style==='narrow' && part.value==='Std.' ? 'h' : part.value.replace(/[ \u00a0\u202f]/g,'')})).filter(part=>part.value!=='');
  console.log(JSON.stringify(value),JSON.stringify(normalized));
 }
}
