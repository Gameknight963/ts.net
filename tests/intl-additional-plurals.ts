/// <reference lib="esnext.intl" />
export {};
const locales=['ar','pl','uk','cs','sk','sl','lt','lv','he','ro'];
// Huge-number operands and current-spec metadata are checked by inspect-plural-rules.
const values=[-103,-23,-1.2,-1,-0,0,0.01,0.1,0.11,0.12,0.21,0.111,0.211,1,1.01,1.1,1.11,1.21,1.5,1.9999,2,2.1,3,4,5,10,11,12,13,14,19,20,21,22,23,25,99,100,101,102,103,111,112,121,1001,1000000,1000000000000001,9007199254740991,Number.MIN_VALUE,NaN,Infinity,-Infinity];
for(const locale of locales)for(const type of ['cardinal','ordinal'] as const) {
    for(const options of [{},{minimumFractionDigits:1},{minimumFractionDigits:2},{minimumFractionDigits:3},{maximumFractionDigits:0},
        {minimumIntegerDigits:3},{minimumSignificantDigits:3},{maximumSignificantDigits:2},
        {maximumFractionDigits:0,roundingMode:'ceil'},{maximumFractionDigits:0,roundingMode:'halfEven'},
        {minimumFractionDigits:1,maximumFractionDigits:1,roundingIncrement:5},
        {maximumSignificantDigits:2,minimumFractionDigits:2,maximumFractionDigits:3,roundingPriority:'morePrecision'},
        {minimumFractionDigits:2,trailingZeroDisplay:'stripIfInteger'}]) {
        const rules=new Intl.PluralRules(locale,{type,...options} as Intl.PluralRulesOptions);
        const resolved=rules.resolvedOptions();
        if('roundingPriority' in options)console.log('priority options',resolved.locale,resolved.type,resolved.pluralCategories.join(','),(resolved as any).roundingPriority);
        else console.log('options',JSON.stringify(resolved));
        for(const value of values)console.log('select',value,rules.select(value));
        const numbers=new Intl.NumberFormat('en-US',{...options,useGrouping:false} as Intl.NumberFormatOptions);
        for(const first of [0,1,2,3,5,11,21,1.2])for(const last of [0,1,2,3,5,11,21,1.2]) {
            // Equal rounded endpoints follow the newer specification; Node uses an older mapping.
            if(numbers.format(first)!==numbers.format(last))console.log('range',first,last,(rules as any).selectRange(first,last));
        }
    }
}
console.log('supported',Intl.PluralRules.supportedLocalesOf(locales).join(','));
console.log('fallback',new Intl.PluralRules(['zz','pl']).resolvedOptions().locale);
