export {};
const locales=['ar-EG','ru','pl','uk','cs','sl','lt','lv','he','ro','it','pt','hi','fa','th','tr','sv'];
for(const locale of locales)for(const style of ['long','short','narrow'] as const)for(const numeric of ['always','auto'] as const) {
    const formatter=new Intl.RelativeTimeFormat(locale,{style,numeric});
    console.log('options',JSON.stringify(formatter.resolvedOptions()));
    for(const unit of ['year','quarter','month','week','day','hour','minute','second'] as const)for(const value of [-2,-1,-0.5,-0.0001,-0,0,0.0001,0.5,1,1.2345,1.5,1.9999,2,3,5,11,21,1234.5,12345.5,Number.MAX_VALUE]) {
        const parts=formatter.formatToParts(value,unit);const text=formatter.format(value,unit);
        if(parts.map(part=>part.value).join('')!==text)throw new Error('Parts do not reconstruct text');
        for(const part of parts) {
            if(typeof part.value!=='string' || part.value.length===0)throw new Error('Invalid part value');
            if(part.type==='literal' ? 'unit' in part : part.unit!==unit)throw new Error('Invalid part unit');
        }
        // Wording and grouping data vary with system ICU versions. Compare exact
        // text for the shared long numeric patterns, and validate all styles above.
        if(style==='long' && numeric==='always' && locale!=='he' && locale!=='fa' && value!==1234.5 && value!==1.2345)
            console.log('relative',unit,String(value),text,JSON.stringify(parts));
    }
}
for(const locale of ['ar-EG','fa','pl','hi'])for(const numberingSystem of ['latn','arab','arabext','deva','mathbold','foobar']) {
    const formatter=new Intl.RelativeTimeFormat(locale,{numberingSystem} as any);
    const parts=formatter.formatToParts(12345.5,'day');
    if(parts.map(part=>part.value).join('')!==formatter.format(12345.5,'day'))throw new Error('Numbering-system parts differ');
    console.log('digits',JSON.stringify(formatter.resolvedOptions()));
    if(locale!=='fa')console.log('digit text',formatter.format(12345.5,'day'),JSON.stringify(parts));
}
console.log('supported',Intl.RelativeTimeFormat.supportedLocalesOf([...locales,'zz']).join(','));
console.log('fallback',new Intl.RelativeTimeFormat(['zz','pl']).resolvedOptions().locale);
for(const locale of ['pl-PL','pl-XX','pl-u-nu-deva','pl-u-nu-foobar','ar-EG-u-nu-latn'])for(const options of [{},{numberingSystem:'latn'},{numberingSystem:'foobar'}]) {
    const formatter=new Intl.RelativeTimeFormat(locale,options as any);
    console.log('resolution',JSON.stringify(formatter.resolvedOptions()),formatter.format(1.5,'day'));
}
let trace='';
const configured=new Intl.RelativeTimeFormat('pl',{
    get localeMatcher(){trace+='m';return 'lookup';},get numberingSystem(){trace+='n';return 'latn';},
    get style(){trace+='s';return 'long';},get numeric(){trace+='u';return 'always';}
} as any);
console.log('getters',trace,configured.format(3,'day'),trace);
