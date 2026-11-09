export {};
function spacing(text:string):string{return text.replace(/[ \u00a0\u202f]/g,'');}
function parts(value:Intl.NumberFormatPart[]):Intl.NumberFormatPart[]{return value.map(part=>({...part,value:part.type==='literal' ? spacing(part.value) : part.value})).filter(part=>part.value!=='');}
for(const locale of ['en-US','ar-EG','fa-IR','bn-BD'])for(const numberingSystem of ['arab','arabext'])for(const extra of [{notation:'engineering'},{minimumIntegerDigits:4},{maximumFractionDigits:1,roundingMode:'ceil'},{maximumFractionDigits:1,roundingMode:'floor'},{signDisplay:'never'},{signDisplay:'negative'},{signDisplay:'exceptZero'},{style:'currency',currency:'USD',currencyDisplay:'code',currencySign:'accounting'},{style:'currency',currency:'USD',currencyDisplay:'narrowSymbol',signDisplay:'always'}] as Intl.NumberFormatOptions[]) {
    const format=new Intl.NumberFormat(locale,{numberingSystem,...extra});
    console.log('options',locale,numberingSystem,JSON.stringify(extra));
    for(const value of [-0,-0.01,0,0.01,-0.125,12345.67,-12345.67,12345678901234567890n,NaN,Infinity,-Infinity]) {
        const text=format.format(value),list=format.formatToParts(value);
        console.log(String(value),spacing(text),JSON.stringify(parts(list)));
        if(list.map(part=>part.value).join('')!==text)throw new Error('Parts differ from text');
    }
}
for(const locale of ['ar-EG','fa-IR','bn-BD'])for(const currency of ['XYZ','ZZZ','BHD','JPY']) {
    const format=new Intl.NumberFormat(locale,{style:'currency',currency});
    console.log('currency',locale,currency,format.resolvedOptions().minimumFractionDigits,spacing(format.format(1234.567)));
}
for(const numberingSystem of ['arab','arabext'])for(const extra of [{style:'unit',unit:'meter'},{style:'currency',currency:'USD',currencyDisplay:'name'}] as Intl.NumberFormatOptions[]) {
    const format=new Intl.NumberFormat('en-US',{numberingSystem,...extra});
    for(const value of [1,1.5,2,-1,NaN,Infinity])console.log('names',numberingSystem,spacing(format.format(value)),JSON.stringify(parts(format.formatToParts(value))));
}
