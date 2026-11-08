/// <reference lib="esnext.intl" />
export {};
for(const tag of ['en-US','en-GB','fr-FR','de-DE','ja-JP','zh-CN','ko-KR','ar-EG','ar-SA','hi-IN','en-IN','ru-RU','es-ES','es-MX','und']) {
 const locale:any=new Intl.Locale(tag);const original=locale.getHourCycles();const format=new Intl.DateTimeFormat(tag,{hour:'numeric'});console.log(tag,original.length,['h11','h12','h23','h24'].includes(original[0]),original[0]===format.resolvedOptions().hourCycle);
 const cycles=locale.getHourCycles();cycles.push('changed');console.log('fresh',locale.getHourCycles().length,locale.getHourCycles()[0]===original[0]);
}
for(const hourCycle of ['h11','h12','h23','h24']) {
 const locale:any=new Intl.Locale('en-US',{hourCycle} as any);
 console.log('option',locale.toString(),locale.hourCycle,locale.getHourCycles().join(','));
 const extended:any=new Intl.Locale('en-GB-u-hc-'+hourCycle);
 console.log('extension',extended.toString(),extended.getHourCycles().join(','));
}
for(const tag of ['en-u-hc-foo']) {
 const locale:any=new Intl.Locale(tag);console.log('unknown',locale.toString(),locale.getHourCycles().join(','));
}
const method=(Intl.Locale.prototype as any).getHourCycles;
console.log('metadata',method.name,method.length);
for(const receiver of [undefined,null,{},'en']) {
 try {method.call(receiver);}catch(error){console.log('receiver',(error as Error).name);}
}

for (const a of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') for (const b of 'ABCDEFGHIJKLMNOPQRSTUVWXYZ') {
 const region = a + b;
 for (const language of ['und', 'en', 'fr', 'ar', 'zh']) {
  const tag = language + '-' + region;
  console.log('region cycle', tag, new Intl.Locale(tag).getHourCycles().join(','));
 }
}
for (const tag of ['en-US-u-rg-gbzzzz', 'en-GB-u-rg-uszzzz', 'fr-u-rg-cazzzz', 'en-u-rg-aazzzz', 'en-u-hc', 'zz', 'zh-Hant', 'pa-Arab']) console.log('override', tag, new Intl.Locale(tag).getHourCycles().join(','));
for (const a of 'abcdefghijklmnopqrstuvwxyz') for (const b of 'abcdefghijklmnopqrstuvwxyz') for (const c of 'abcdefghijklmnopqrstuvwxyz') {
 const tag = a + b + c;
 console.log('language cycle', tag, new Intl.Locale(tag).getHourCycles().join(','));
}

for (const tag of ['zh-CN', 'es-MX', 'fr-CA', 'en-US-u-rg-gbzzzz', 'en-GB-u-rg-uszzzz']) for (const options of [{hour: 'numeric'}, {timeStyle: 'short'}, {hour: 'numeric', hour12: true}, {hour: 'numeric', hour12: false}]) {
 const f = new Intl.DateTimeFormat(tag, {timeZone: 'UTC', ...options} as any);
 for (const time of ['2025-01-01T01:00:00Z', '2025-01-01T13:00:00Z']) {
  const parts = f.formatToParts(new Date(time));
  const hours = Number(parts.find(part => part.type === 'hour')?.value);
  console.log('date cycle', tag, JSON.stringify(options), f.resolvedOptions().hourCycle, f.resolvedOptions().hour12, hours, parts.some(part => part.type === 'dayPeriod'));
 }
}
