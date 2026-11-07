const timestamp=Date.UTC(2024,1,29,13,4,5,678);
for(const locale of ['en-US','en-GB']) {
 const formatter=new Intl.DateTimeFormat(locale,{timeZone:'UTC'});
 console.log('default',locale,formatter.format(timestamp));
 console.log('resolved-default',JSON.stringify(formatter.resolvedOptions()));
}
const date=new Intl.DateTimeFormat('en-US',{timeZone:'UTC',year:'numeric',month:'long',day:'numeric'});
console.log('date',date.format(timestamp),date.format(new Date(timestamp)),date.format('0' as any));
const time=new Intl.DateTimeFormat('en-GB',{timeZone:'UTC',hour:'2-digit',minute:'2-digit',second:'2-digit',hourCycle:'h23',fractionalSecondDigits:3});
console.log('time',time.format(timestamp));
const resolved=time.resolvedOptions();console.log('time-options',resolved.hourCycle,resolved.hour12,resolved.hour,resolved.minute,resolved.second,resolved.fractionalSecondDigits);
const format=date.format;console.log('bound',format===date.format,format(timestamp),format.call(null,timestamp));
console.log('callback',[timestamp,timestamp+86400000].map(format).join('|'));
console.log('callable',Intl.DateTimeFormat('en-US',{timeZone:'UTC',year:'numeric'}).format(timestamp));
class Formatter extends Intl.DateTimeFormat {marker=7;}
const child=new Formatter('en-US',{timeZone:'UTC',year:'numeric'});
console.log('derived',child.marker,child.format(timestamp),child instanceof Formatter,child instanceof Intl.DateTimeFormat);
console.log('supported',Intl.DateTimeFormat.supportedLocalesOf(['EN-us','en-US','de-DE','zz']).join(','));
console.log('tag',Object.prototype.toString.call(date));
console.log('metadata',Intl.DateTimeFormat.name,Intl.DateTimeFormat.length,date.format.name,date.format.length,date.resolvedOptions.name,date.resolvedOptions.length);
let reads=0;const options={get year(){reads++;return 'numeric' as const;},get timeZone(){reads++;return 'UTC';}};
const cached=new Intl.DateTimeFormat('en-US',options);const after=reads;
console.log('cached',cached.format(timestamp),cached.format(timestamp),reads===after);
const mutable:Intl.DateTimeFormatOptions={year:'numeric',timeZone:'UTC'};const frozen=new Intl.DateTimeFormat('en-US',mutable);mutable.year='2-digit';console.log('snapshot',frozen.format(timestamp),frozen.resolvedOptions().year);
const own=date.resolvedOptions();own.year='2-digit';console.log('fresh-options',date.resolvedOptions().year);
for(const value of [NaN,Infinity,new Date(NaN)])try{date.format(value as any);}catch(error){console.log('invalid-value',(error as Error).name);}
for(const option of [{year:'invalid'},{dateStyle:'long',year:'numeric'},{timeZone:'No/Such_Zone'},{hourCycle:'invalid'}])try{new Intl.DateTimeFormat('en-US',option as Intl.DateTimeFormatOptions);}catch(error){console.log('invalid-option',(error as Error).name);}
const getter=Object.getOwnPropertyDescriptor(Intl.DateTimeFormat.prototype,'format')!.get!;
try{getter.call({});}catch(error){console.log('receiver',(error as Error).name);}
try{date.resolvedOptions.call({});}catch(error){console.log('receiver',(error as Error).name);}
console.log('now',typeof date.format()==='string',date.format().length>0);

console.log('native-default',new Intl.DateTimeFormat('de-DE',{timeZone:'UTC'}).format(timestamp)===new Date(timestamp).toLocaleDateString('de-DE',{timeZone:'UTC'}));
for(const style of ['short','medium','long','full'] as const) {
 console.log('date-style',style,new Intl.DateTimeFormat('en-US',{timeZone:'UTC',dateStyle:style}).format(timestamp)===new Date(timestamp).toLocaleDateString('en-US',{timeZone:'UTC',dateStyle:style}));
 console.log('time-style',style,new Intl.DateTimeFormat('en-GB',{timeZone:'UTC',timeStyle:style}).format(timestamp)===new Date(timestamp).toLocaleTimeString('en-GB',{timeZone:'UTC',timeStyle:style}));
}

console.log('native-options',new Intl.DateTimeFormat('en-US',{timeZone:'UTC',calendar:'gregory',numberingSystem:'latn',year:'numeric'}).format(timestamp));
console.log('date-native-options',new Date(timestamp).toLocaleDateString('en-US',{timeZone:'UTC',calendar:'gregory',numberingSystem:'latn',year:'numeric'}));

try{structuredClone(date);}catch(error){console.log('clone',(error as Error).name);}
const alternate=Reflect.construct(Intl.DateTimeFormat,['en-US',{timeZone:'UTC',year:'numeric'}],Formatter);
console.log('reflect',alternate.format(timestamp),Object.getPrototypeOf(alternate)===Formatter.prototype);

const Constructor=Intl.DateTimeFormat;
console.log('constructor-alias',new Constructor('en-US',{timeZone:'UTC',year:'numeric'}).format(timestamp));
