export {};
const cases=[
    ['en-US','emoji',['\u{1f600}','\u{1f603}','\u{1f436}','\u{1f431}','\u{1f34e}','\u{1f34c}']],
    ['en-US','eor',['z','\u00e4','a','\u00f6','o','\u00fc','u']],
    ['zh','pinyin',['\u738b','\u5f20','\u674e','\u8d75','\u516b']],
    ['zh','unihan',['\u738b','\u5f20','\u674e','\u8d75','\u516b']],
    ['ko','searchjl',['\u3131','\uac00','\uac01','\ub098','\ub2e4']],
    ['si','dict',['\u0d85','\u0d86','\u0d87','\u0d9a','\u0d9b']],
    ['si','compat',['\u0d85','\u0d86','\u0d87','\u0d9a','\u0d9b']],
    ['zh','phonetic',['\u738b','\u5f20','\u674e','\u8d75','\u516b']]
] as const;
for(const [locale,collation,words] of cases){
    const f=new Intl.Collator(locale,{collation});console.log('collation',locale,f.resolvedOptions().collation,[...words].sort(f.compare).join('|'));
    console.log('available',Intl.supportedValuesOf('collation').includes(collation));
}
for(const collation of ['foobar','unknown','phonebk','search','standard','big5han','gb2312','reformed']){
    const f=new Intl.Collator('en-US',{collation:collation as any});console.log('fallback',collation,f.resolvedOptions().collation,f.compare('a','b'));
}
for(const locale of ['en-US-u-co-foobar','en-US-u-co-emoji','en-US-u-kn','en-US-u-kn-false','da-u-kf-upper']){
    for(const options of [{},{collation:'eor'},{numeric:true},{numeric:false},{caseFirst:'lower'}] as Intl.CollatorOptions[]){
        const f=new Intl.Collator(locale,options);const r=f.resolvedOptions();console.log('resolved',r.locale,r.collation,r.numeric,r.caseFirst);
    }
}
for(const sensitivity of ['base','accent','case','variant'] as const){
    const f=new Intl.Collator('en-US',{sensitivity,caseFirst:'upper'});
    console.log('sensitivity',sensitivity,f.compare('a','\uff41'),f.compare('A','a'),f.compare('\u00e4','a'));
}
for(const ignorePunctuation of [undefined,false,true]){
    const f=new Intl.Collator('th',{ignorePunctuation});console.log('thai',f.resolvedOptions().ignorePunctuation,f.compare('a-b','ab'),'a-b'.localeCompare('ab','th',{ignorePunctuation}));
}
console.log('thai default','a-b'.localeCompare('ab','th'));
const symbols=new Intl.Collator('en',{ignorePunctuation:true});console.log('symbols',symbols.compare('a-b','ab'),symbols.compare('a$b','ab'));
let enabled=false,reads=0;
const changing={collation:'emoji',get numeric(){reads++;return enabled;}} as Intl.CollatorOptions;
for(const value of [false,true,false,true]){enabled=value;console.log('changed option','file2'.localeCompare('file10','en-US',changing));}
console.log('option reads',reads);
const unicode=new Intl.Collator('en-US');
console.log('unicode',unicode.compare('a\u0315\u0300','\u00e0\u0315'),unicode.compare('a\0z','ay'),unicode.compare('\ud800a','\ud800b'));
