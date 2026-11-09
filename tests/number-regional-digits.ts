export {};
// CLR and ICU tables can use different affix whitespace. Preserve numeric
// grouping, punctuation and directional marks in the comparison.
function comparable(parts:Intl.NumberFormatPart[]):Intl.NumberFormatPart[]{return parts.map(part=>({...part,value:part.type==='literal' ? part.value.replace(/[ \u00a0\u202f]/g,'') : part.value})).filter(part=>part.value!=='');}
function spacing(text:string):string{return text.replace(/[ \u00a0\u202f]/g,'');}
for(const locale of ['en-US','de-DE','ar','ar-EG','ar-SA','ar-AE','ar-MA','fa-IR','bn-BD','mr-IN','ne-NP','my-MM','pa-Arab-PK'])for(const numberingSystem of [undefined,'latn','arab','arabext','deva','foobar']) {
    for(const extra of [{},{style:'percent'},{style:'currency',currency:'USD'},{notation:'compact'},{notation:'scientific'},{signDisplay:'always'}] as Intl.NumberFormatOptions[]) {
        const format=new Intl.NumberFormat(locale,{numberingSystem,...extra});
        console.log('options',locale,numberingSystem,JSON.stringify(extra),format.resolvedOptions().numberingSystem,format.resolvedOptions().locale);
        for(const value of [0,-0,0.125,12345.67,-12345.67,12345678901234567890n,Infinity,-Infinity,NaN]) {
            const text=format.format(value),parts=format.formatToParts(value);
            console.log(String(value),spacing(text),JSON.stringify(comparable(parts)));
            if(parts.map(part=>part.value).join('')!==text)throw new Error('Number parts differ from text');
        }
    }
    const format=new Intl.NumberFormat(locale,{numberingSystem});
    console.log('methods',spacing((12345.67).toLocaleString(locale,{numberingSystem})),spacing((12345678901234567890n).toLocaleString(locale,{numberingSystem})));
}

let trace='';
const repeated={get numberingSystem(){trace+='get|';return {toString(){trace+='string|';return 'arab';}};}};
for(let i=0;i<4;i++)console.log('repeated',new Intl.NumberFormat('ar-EG',repeated as any).format(12.5));
console.log('repeated-options',trace);
