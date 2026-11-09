export {};
for(const locale of ['en-US','de-DE','ar','ar-SA','ar-EG','ar-AE','fa-IR','th-TH','bn-BD','mr-IN','ne-NP','my-MM','zh-CN','pa-Arab-PK']) {
    const format=new Intl.DateTimeFormat(locale,{timeZone:'UTC',year:'numeric',month:'2-digit',day:'2-digit'});
    const resolved=format.resolvedOptions();
    console.log(locale,resolved.calendar,resolved.numberingSystem);
    for(const value of [0,Date.UTC(2024,1,29)]) {
        const parts=format.formatToParts(value);
        console.log(JSON.stringify(parts.filter(part=>part.type!=='literal' && part.type!=='era')));
        if(parts.map(part=>part.value).join('')!==format.format(value))throw new Error('Parts differ from text');
    }
}
