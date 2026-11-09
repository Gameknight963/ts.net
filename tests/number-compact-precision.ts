export {};
const options:Intl.NumberFormatOptions[]=[{numberingSystem:'deva'},{numberingSystem:'mathbold'},{minimumIntegerDigits:4},{useGrouping:'always'},{useGrouping:false},{minimumFractionDigits:2,maximumFractionDigits:2},{minimumSignificantDigits:4,maximumSignificantDigits:4},{minimumFractionDigits:1,maximumFractionDigits:1,minimumSignificantDigits:2,maximumSignificantDigits:2,roundingPriority:'morePrecision'},{minimumFractionDigits:1,maximumFractionDigits:1,minimumSignificantDigits:2,maximumSignificantDigits:2,roundingPriority:'lessPrecision'},{maximumFractionDigits:2,trailingZeroDisplay:'stripIfInteger'},{maximumFractionDigits:2,roundingIncrement:25},{maximumFractionDigits:0,roundingMode:'ceil'},{maximumFractionDigits:0,roundingMode:'floor'},{maximumFractionDigits:0,roundingMode:'trunc'},{maximumFractionDigits:0,roundingMode:'expand'},{maximumFractionDigits:0,roundingMode:'halfEven'},{maximumFractionDigits:0,roundingMode:'halfCeil'},{maximumFractionDigits:0,roundingMode:'halfFloor'},{maximumFractionDigits:0,roundingMode:'halfTrunc'},{style:'percent'},{style:'currency',currency:'EUR'},{style:'currency',currency:'USD',currencyDisplay:'code',currencySign:'accounting'},{signDisplay:'negative'},{signDisplay:'exceptZero'}];
for(const locale of ['de-DE','ja-JP','fr-FR','pl-PL','hi-IN','es-ES'])for(const compactDisplay of ['short','long'] as const)for(const extra of options) {
    let format:Intl.NumberFormat;
    try{format=new Intl.NumberFormat(locale,{notation:'compact',compactDisplay,...extra});}catch(error){console.log('invalid',locale,compactDisplay,JSON.stringify(extra),(error as Error).name);continue;}
    console.log('options',locale,compactDisplay,JSON.stringify(extra));
    for(const value of [0,-0,0.001,999.4,999.5,9999.5,999500,999950,1234500,1000000,-999.5,-999500,9007199254740993n,123456789012345678901234567890123456789n,'1234567.890123456789','999999.999999999999']) {
        const parts=format.formatToParts(value as any),text=format.format(value as any);
        console.log(String(value),text,JSON.stringify(parts));
        if(parts.map(part=>part.value).join('')!==text)throw new Error('Parts differ from text');
    }
}

const cached=new Intl.NumberFormat('de-DE',{notation:'compact',maximumFractionDigits:1});
let conversions=0,fail=false;
const input={valueOf(){conversions++;if(fail)throw new Error('coercion failed');return 1200+conversions;}};
console.log('coercion',cached.format(input as any),cached.format(input as any),JSON.stringify(cached.formatToParts(input as any)),conversions);
fail=true;
try{cached.format(input as any);}catch(error){console.log('coercion-error',(error as Error).message,conversions);}
