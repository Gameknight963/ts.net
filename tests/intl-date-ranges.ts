export {};
// System ICU versions differ in spacing around the interval separator.
const start=Date.UTC(2024,0,3,9,15,20,123);
const ends=[start,start+1,start+3600000,start+86400000,start+40*86400000,Date.UTC(2025,1,5,18,45,22,987)];
for(const locale of ['en-US','en-GB']){
    for(const options of [
        {year:'numeric',month:'short',day:'numeric'},
        {year:'numeric',month:'long',day:'numeric'},
        {hour:'2-digit',minute:'2-digit'},
        {hour:'2-digit',minute:'2-digit',second:'2-digit',fractionalSecondDigits:3,hourCycle:'h23'}
    ] as Intl.DateTimeFormatOptions[]){
        const formatter=new Intl.DateTimeFormat(locale,{...options,timeZone:'UTC'});
        for(const end of ends){
            if(options.fractionalSecondDigits && end===start+1)continue;
            console.log(locale,JSON.stringify(options),end-start,formatter.formatRange(start,end).replace(/\s/g,''));
        }
    }
}

for(const locale of ['en-US','de-DE','ja-JP']) {
    for(const dateStyle of ['full','long','medium','short'] as const) {
        const f=new Intl.DateTimeFormat(locale,{timeZone:'UTC',dateStyle,timeStyle:'short'});
        const a=f.formatRange(start,start),b=f.formatRange(start,start+86400000);
        console.log('styles',locale,dateStyle,a===f.format(start),b.length>0,b===f.formatRange(start,start+86400000));
    }
}
