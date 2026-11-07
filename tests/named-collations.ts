const orders = [
 {locale:'de-DE',collation:'phonebk',words:['z','\u00e4','ae','\u00f6','oe','\u00fc','ue','a','A','a2','A10']},
 {locale:'de-AT',collation:'phonebk',words:['z','\u00e4','ae','\u00f6','oe','\u00fc','ue']},
 {locale:'es-ES',collation:'trad',words:['ca','ce','ch','ci','cz','da','la','ll','lz','ma','a','A']},
 {locale:'es-MX',collation:'trad',words:['ca','ce','ch','ci','cz','da','la','ll','lz','ma']},
 {locale:'zh-CN',collation:'stroke',words:['\u963f','\u516b','\u4e01','\u4e2d','\u56fd','\u5f20','\u674e','\u738b','a','A']},
 {locale:'zh-TW',collation:'zhuyin',words:['\u963f','\u516b','\u4e01','\u4e2d','\u56fd','\u5f20','\u674e','\u738b']}
] as const;
for(const order of orders) for(const sensitivity of ['base','accent','case','variant'] as const)for(const caseFirst of ['upper','lower','false'] as const) {
 const options:Intl.CollatorOptions={collation:order.collation,sensitivity,caseFirst,numeric:true};
 const collator=new Intl.Collator(order.locale,options);
 console.log(order.locale,sensitivity,caseFirst,collator.resolvedOptions().collation,[...order.words].sort(collator.compare).join('|'));
 for(let i=1;i<order.words.length;i++)console.log('direct',Math.sign(order.words[i-1].localeCompare(order.words[i],order.locale,options)));
}
for(const collation of ['phonebk','trad','stroke','zhuyin'] as const) {
 const collator=new Intl.Collator('en-US',{collation});console.log('fallback',collation,collator.resolvedOptions().collation,collator.compare('a','b'));
}
const search=new Intl.Collator('de-DE',{collation:'phonebk',usage:'search',sensitivity:'base'});
console.log('search',search.resolvedOptions().collation,search.compare('\u00e4','ae'));
const upper=new Intl.Collator('de-DE',{collation:'PHONEBK' as any});console.log('canonical',upper.resolvedOptions().collation);
for(const collation of ['x','bad%','abc--def','abcdefghi'])try{new Intl.Collator('en-US',{collation:collation as any});}catch(error){console.log('invalid',(error as Error).name);}

for(const locale of ['zh-CN','zh-TW'])console.log('locale-alias',locale,new Intl.Collator(locale).resolvedOptions().locale,new Intl.DateTimeFormat(locale,{timeZone:'UTC'}).resolvedOptions().locale);

for(const [locale,collation] of [['de','phonebk'],['es','trad'],['zh','stroke'],['zh-Hant','zhuyin']] as const) {
 const collator=new Intl.Collator(locale,{collation});console.log('neutral',locale,collator.resolvedOptions().collation,collator.compare('a','b'));
}
