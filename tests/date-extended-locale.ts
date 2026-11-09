export {};
// ICU versions differ in the zero-offset label and Thai style joining text.
function normalize(value:string):string{return value.replace(/\s/g,' ').replace(/GMT\+00:00/g,'GMT').replace(/ เวลา /g,' ');}
const values=[-8640000000000000,8640000000000000,-62167219200000,Date.UTC(10000,0,2,3,4,5,678)];
for(const locale of ['en-US','en-GB','de-DE','ja-JP','th-TH'])for(const options of [
    {year:'numeric',month:'2-digit',day:'2-digit'},
    {weekday:'long',era:'short',year:'numeric',month:'long',day:'numeric'},
    {hour:'2-digit',minute:'2-digit',second:'2-digit',fractionalSecondDigits:3,hourCycle:'h23'},
    {dateStyle:'full',timeStyle:'long'},
    {dateStyle:'short',timeStyle:'short'}
]) {
    const formatter=new Intl.DateTimeFormat(locale,{...options,timeZone:'UTC'} as any);
    for(const value of values) {
        const parts=formatter.formatToParts(value);
        if(normalize(parts.map(part=>part.value).join(''))!==normalize(formatter.format(value)))throw new Error('Date parts do not reconstruct the text');
        console.log(locale,value,normalize(formatter.format(value)),JSON.stringify(parts.map(part=>({type:part.type,value:normalize(part.value)}))));
    }
}
for(const hourCycle of ['h11','h12','h23','h24']) {
    const formatter=new Intl.DateTimeFormat('en-US',{timeZone:'UTC',hour:'numeric',minute:'2-digit',hourCycle} as any);
    for(const value of [Date.UTC(10000,0,1),Date.UTC(10000,0,1,12),Date.UTC(10000,0,1,23,59)])console.log('cycle',hourCycle,normalize(formatter.format(value)),JSON.stringify(formatter.formatToParts(value).map(part=>({type:part.type,value:normalize(part.value)}))));
}
for(const timeZone of ['UTC','+05:30','-03:45','+23:59']) {
    const formatter=new Intl.DateTimeFormat('en-US',{timeZone,year:'numeric',month:'2-digit',day:'2-digit',hour:'2-digit',minute:'2-digit',hourCycle:'h23',timeZoneName:'longOffset'});
    for(const value of values)console.log('zone',timeZone,value,normalize(formatter.format(value)));
}
for(const value of values) {
    const date=new Date(value);
    console.log('prototype',value,normalize(date.toLocaleString('en-US',{timeZone:'UTC'})),normalize(date.toLocaleDateString('en-US',{timeZone:'UTC'})),normalize(date.toLocaleTimeString('en-US',{timeZone:'UTC'})));
}
