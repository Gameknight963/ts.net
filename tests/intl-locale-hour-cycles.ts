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
