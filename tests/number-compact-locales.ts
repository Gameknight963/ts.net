export {};
for(const locale of ['en-GB','de-DE','ja-JP','zh-CN','ko-KR','hi-IN','pl-PL','cs-CZ','ru-RU','es-ES','fr-FR','es-MX','pt-BR','pt-PT','it-IT','nl-NL','tr-TR','bn-BD','sv-SE','da-DK','fi-FI','he-IL','uk-UA','id-ID','vi-VN','sw-KE','fa-IR','ar-EG'])for(const compactDisplay of ['short','long'] as const) {
    for(const extra of [{},{maximumFractionDigits:2},{minimumFractionDigits:1,maximumFractionDigits:1},{minimumSignificantDigits:3,maximumSignificantDigits:3},{signDisplay:'always'},{useGrouping:false}] as Intl.NumberFormatOptions[]) {
        const format=new Intl.NumberFormat(locale,{notation:'compact',compactDisplay,numberingSystem:'latn',...extra});
        console.log('options',locale,compactDisplay,JSON.stringify(extra),JSON.stringify(format.resolvedOptions()));
        for(const value of [0,-0,12,999,1000,1200,2000,5000,9999,10000,12345,999500,1000000,1200000,2000000,5000000,100000000,1000000000,1000000000000,1e16,1e18,-1200,Infinity,NaN]) {
            const parts=format.formatToParts(value);
            console.log('value',String(value),format.format(value),JSON.stringify(parts));
            if(parts.map(part=>part.value).join('')!==format.format(value))throw new Error('Parts differ from text');
        }
    }
}
