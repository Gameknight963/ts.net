export {};
for(const locale of ['fi-FI','da-DK','ar-EG','fa-IR','bn-BD','hi-IN','pl-PL','cs-CZ','he-IL','ko-KR','zh-CN'])for(const extra of [{},{fractionalDigits:0},{fractionalDigits:3},{fractionalDigits:6},{fractionalDigits:9},{hours:'2-digit'},{numberingSystem:'latn'},{numberingSystem:'deva'},{hoursDisplay:'auto',minutesDisplay:'auto',secondsDisplay:'auto'}] as Intl.DurationFormatOptions[]) {
 // Node currently ignores the explicit latn option in native-digit locales;
 // use its equivalent locale extension for this differential comparison.
 const selected=extra.numberingSystem==='latn' ? locale+'-u-nu-latn' : locale;
 const f=new Intl.DurationFormat(selected,{style:'digital',...extra});console.log(locale,JSON.stringify(extra),JSON.stringify(f.resolvedOptions()));
 for(const value of [{seconds:0},{hours:1,minutes:2,seconds:3},{hours:-1,seconds:-1},{seconds:1,milliseconds:234,microseconds:567,nanoseconds:891},{milliseconds:999},{seconds:0,milliseconds:-1},{nanoseconds:1},{seconds:60},{hours:25,minutes:90,seconds:120}]) {
  const text=f.format(value),parts=f.formatToParts(value);if(parts.map(part=>part.value).join('')!==text)throw new Error('Digital parts differ from text');
  console.log(JSON.stringify(value),text,JSON.stringify(parts));
 }
}
