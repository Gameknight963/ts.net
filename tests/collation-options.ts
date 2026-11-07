for(const locale of ['en-US','de-DE','da-DK','tr-TR','el-GR']) {
 for(const caseFirst of ['upper','lower','false'] as const) {
  for(const sensitivity of ['base','accent','case','variant'] as const) {
   for(const usage of ['sort','search'] as const) {
    const options:Intl.CollatorOptions={caseFirst,sensitivity,usage,numeric:true,ignorePunctuation:true,localeMatcher:'lookup' as const,collation:'default'};
    const collator=new Intl.Collator(locale,options);
    console.log('options',locale,caseFirst,sensitivity,usage,collator.resolvedOptions().caseFirst);
    for(const [left,right] of [['a','A'],['a\u00e1','A\u00e1'],['I','\u0131'],['\u00df','\u1e9e'],['file02','File2'],['a-b','Ab'],['\u00e4','ae']])
     console.log(left,right,Math.sign(collator.compare(left,right)),Math.sign(left.localeCompare(right,locale,options)));
   }
  }
 }
}
for(const locale of ['en-US','da-DK'])for(const usage of ['sort','search'] as const)console.log('default-case',locale,usage,new Intl.Collator(locale,{usage}).resolvedOptions().caseFirst);
const mutable={caseFirst:'upper' as 'upper'|'lower'};const cached=new Intl.Collator('en-US',mutable);mutable.caseFirst='lower';console.log('snapshot',cached.compare('A','a'),cached.resolvedOptions().caseFirst);
for(const options of [{caseFirst:'invalid'},{usage:'invalid'},{localeMatcher:'invalid'},{sensitivity:'invalid'}])try{'a'.localeCompare('A','en-US',options as Intl.CollatorOptions);}catch(error){console.log('invalid',(error as Error).name);}
