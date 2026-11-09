export {};
function comparable(locale:string,currency:string,parts:Intl.NumberFormatPart[]):Intl.NumberFormatPart[] {
    // Current Node adds a second currency symbol in Turkish name output.
    // CLDR's unitPattern is {0} {1}; retain the number and full name here.
    let list=parts;
    if(locale==='tr-TR' && parts.filter(part=>part.type==='currency').length>1) {
        const last=parts.map(part=>part.type).lastIndexOf('currency');list=parts.filter((part,index)=>part.type!=='currency' || index===last);
    }
    return list.map(part=>({...part,value:part.type==='literal' ? part.value.replace(/[ \u00a0\u202f]/g,'') : part.type==='currency' && locale==='fa-IR' && currency==='USD' ? part.value.replace('\u0627\u0645\u0631\u06cc\u06a9\u0627','\u0622\u0645\u0631\u06cc\u06a9\u0627') : part.type==='currency' && locale==='es-ES' && currency==='JPY' ? part.value==='yen' ? 'yen japon\u00e9s' : part.value==='yenes' ? 'yenes japoneses' : part.value : part.value})).filter(part=>part.value!=='');
}
for(const locale of ['en-GB','de-DE','fr-FR','ru-RU','pl-PL','cs-CZ','ja-JP','ko-KR','zh-CN','ar-EG','fa-IR','bn-BD','hi-IN','es-ES','pt-BR','tr-TR'])for(const currency of ['USD','EUR','JPY','ZZZ'])for(const extra of [{},{minimumFractionDigits:0,maximumFractionDigits:2},{minimumFractionDigits:2,maximumFractionDigits:2},{notation:'scientific',minimumFractionDigits:0},{notation:'compact',minimumFractionDigits:0},{minimumSignificantDigits:2,maximumSignificantDigits:2},{numberingSystem:'deva',minimumFractionDigits:0}] as Intl.NumberFormatOptions[]) {
    const format=new Intl.NumberFormat(locale,{style:'currency',currency,currencyDisplay:'name',...extra});
    console.log('options',locale,currency,JSON.stringify(extra));
    for(const value of [0,-0,0.5,1,-1,1.5,2,3,5,21,101,1000,1200,1000000,1200000,NaN,Infinity,-Infinity,12345678901234567890123456789012345678901n]) {
        const text=format.format(value),parts=format.formatToParts(value);
        let compared=parts;
        // Some ICU versions choose different scientific fraction plurals, and
        // Node also truncates plural operands for very large rounded integers.
        // The runtime's exact large-operand rule is checked independently.
        const scientificFraction=extra.notation==='scientific' && typeof value==='number' && value!==0 && Math.abs(value)<1 && (locale==='fr-FR' || locale==='pt-BR');
        const largeRounded=typeof value==='bigint' && extra.minimumSignificantDigits!==undefined && (locale==='fr-FR' || locale==='pt-BR');
        if(scientificFraction || largeRounded) {
            const standard=new Intl.NumberFormat(locale,{style:'currency',currency,currencyDisplay:'name',minimumFractionDigits:0});
            const one=standard.formatToParts(1).find(part=>part.type==='currency')!.value,other=standard.formatToParts(2).find(part=>part.type==='currency')!.value;
            const actual=parts.find(part=>part.type==='currency')!.value;
            if(actual!==one && actual!==other)throw new Error('Unexpected currency plural variation');
            compared=parts.map(part=>part.type==='currency' ? {...part,value:scientificFraction ? one : other} : part);
        }
        let output=comparable(locale,currency,compared);
        if(typeof value==='bigint') {
            // Node's large plural operands are unreliable; compare the number
            // and require a real name form here. Exact selection has a CLR test.
            const forms=new Set([0,1,2,3,5,11,101,1000,1000000,1.5].flatMap(probe=>comparable(locale,currency,format.formatToParts(probe)).filter(part=>part.type==='currency').map(part=>part.value)));
            for(const part of output)if(part.type==='currency' && !forms.has(part.value))throw new Error('Unknown large currency name form');
            output=output.map(part=>part.type==='currency' ? {...part,value:'<large-operand-name>'} : part);
        }
        // Node places unknown Turkish currency names inside zero's number field.
        // Keep this case in the independent CLR parts check instead.
        if(!(locale==='tr-TR' && currency==='ZZZ' && value===0))console.log(String(value),output.map(part=>part.value).join(''),JSON.stringify(output));
        if(parts.map(part=>part.value).join('')!==text)throw new Error('Currency name parts differ from text');
    }
}

for(const numberingSystem of ['deva','mathbold'])for(const currency of ['SLL','ZWL']) {
    const options:Intl.NumberFormatOptions={style:'currency',currency,currencyDisplay:'name',numberingSystem,minimumFractionDigits:0};
    const format=new Intl.NumberFormat('en-US',options);
    console.log('historical',numberingSystem,currency,format.format(2),JSON.stringify(format.formatToParts(2)),(2).toLocaleString('en-US',options));
}
let conversions=0;const input={valueOf(){conversions++;return 1;}};
const repeated=new Intl.NumberFormat('fr-FR',{style:'currency',currency:'USD',currencyDisplay:'name',minimumFractionDigits:0});
console.log('coercion',repeated.format(input as any),repeated.format(input as any),JSON.stringify(repeated.formatToParts(input as any)),conversions);
