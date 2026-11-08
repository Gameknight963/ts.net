export {};
for(const tag of ['en-US','en-GB','fr-FR','de-DE','ja-JP','zh-CN','ko-KR','ar-EG','ar-SA','he-IL','fa-IR','th-TH']) {
 const locale:any=new Intl.Locale(tag), calendars:string[]=locale.getCalendars();
 const primary=new Intl.DateTimeFormat(tag).resolvedOptions().calendar;
 console.log(tag,calendars.length>0,new Set(calendars).size===calendars.length,calendars.includes(primary));
 const before=calendars.join(',');calendars.push('changed');console.log('fresh',locale.getCalendars().join(',')===before);
}
for(const calendar of ['gregory','japanese','buddhist','hebrew','persian','islamic','islamic-umalqura','unknown']) {
 const locale:any=new Intl.Locale('en-US',{calendar});
 console.log('option',locale.toString(),locale.getCalendars().join(','));
 const extended:any=new Intl.Locale('en-u-ca-'+calendar);console.log('extension',extended.getCalendars().join(','));
}
const method=(Intl.Locale.prototype as any).getCalendars;
console.log('metadata',method.name,method.length);
for(const receiver of [undefined,null,{},'en']) {
 try {method.call(receiver);}catch(error){console.log('receiver',(error as Error).name);}
}
