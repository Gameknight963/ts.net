export {};
function normalize(value:string):string{return value.replace(/\s/g,' ');}
for(const calendar of ['chinese','dangi','coptic','ethiopic','ethioaa','indian','islamic','islamic-civil','islamic-rgsa','islamic-tbla','islamic-umalqura','japanese','buddhist','hebrew','persian','roc','gregory','iso8601']) {
    const formatter=new Intl.DateTimeFormat('en-US',{calendar,timeZone:'UTC',year:'numeric',month:'2-digit',day:'2-digit'});
    console.log('calendar',calendar,formatter.resolvedOptions().calendar,Intl.supportedValuesOf('calendar').includes(calendar));
    const resolved=formatter.resolvedOptions();
    console.log('metadata',JSON.stringify(Object.keys(resolved).sort().map(key=>[key,(resolved as any)[key]])));
    for(const value of [0,Date.UTC(2024,1,29,12,34,56),Date.UTC(10000,0,1),-62167219200000]) {
        // Old system ICU has invalid month calculations for these ancient
        // dates. inspect-extended-dates checks the guard and corrected newer ICU.
        if(value===-62167219200000 && (calendar==='indian' || calendar==='islamic' || calendar==='islamic-rgsa'))continue;
        const parts=formatter.formatToParts(value);
        // Compare calendar fields separately from era spelling, which varies
        // between the system ICU data and Node's bundled ICU data.
        console.log('value',value,JSON.stringify(parts.filter(part=>part.type!=='literal' && part.type!=='era').map(part=>({type:part.type,value:normalize(part.value)}))));
        if(normalize(parts.map(part=>part.value).join(''))!==normalize(formatter.format(value)))throw new Error('Calendar parts differ from text');
    }
}
for(const calendar of ['islamicc','ethiopic-amete-alem','GREGORY','foobar']) {
    const formatter=new Intl.DateTimeFormat('th-TH',{calendar,timeZone:'UTC',year:'numeric',month:'2-digit',day:'2-digit'});
    console.log('resolve',calendar,formatter.resolvedOptions().calendar,JSON.stringify(formatter.formatToParts(0).filter(part=>part.type!=='literal' && part.type!=='era')));
}
for(const calendar of ['','a','gregorian','bad_name']) {
    try{new Intl.DateTimeFormat('en-US',{calendar});}catch(error){console.log('invalid',calendar,(error as Error).name);}
}
