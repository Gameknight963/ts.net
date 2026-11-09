/// <reference lib="es2023.intl" />
export {};
for(const locale of ['en-GB','de-DE','fr-FR','ru-RU','ja-JP','zh-CN','ar-EG','fa-IR','pl-PL','hi-IN'])for(const extra of [{},{maximumFractionDigits:0},{minimumFractionDigits:2,maximumFractionDigits:2},{numberingSystem:'deva'},{signDisplay:'always'},{signDisplay:'never'},{useGrouping:false}] as Intl.NumberFormatOptions[]) {
 const f=new Intl.NumberFormat(locale,extra);console.log(locale,JSON.stringify(extra));
 for(const pair of [[1,2],[1,1],[1.1,1.2],[-0,0],[-0,-0],[-2,-1],[1234.5,6789.25],[Infinity,Infinity],[-Infinity,Infinity],[2,1]]) {
  const text=f.formatRange(pair[0],pair[1]),parts=f.formatRangeToParts(pair[0],pair[1]);
  if(parts.map(part=>part.value).join('')!==text)throw new Error('Regional range parts differ from text');
  // French's approximation symbol changed between CLDR releases.
  console.log(pair.map(String).join(','),JSON.stringify(parts.map(part=>({...part,value:part.type==='approximatelySign' && locale==='fr-FR' ? '\u2248' : part.type==='literal' ? part.value.replace(/[ \u00a0\u202f]/g,'') : part.value})).filter(part=>part.value!=='')));
 }
}

const f=new Intl.NumberFormat('de-DE');const trace:string[]=[];
const first={valueOf(){trace.push('first');return 1;}},second={valueOf(){trace.push('second');return 2;}};
console.log('coercion',f.formatRange(first as any,second as any),trace.join(','));
for(const pair of [[undefined,1],[1,undefined],[NaN,1],[1,NaN],[Symbol(),1]])try{console.log('unexpected',f.formatRange(pair[0] as any,pair[1] as any));}catch(error){console.log('invalid',(error as Error).name);}
const parts=f.formatRangeToParts(1,2);parts[0].value='mutated';console.log('fresh',f.formatRangeToParts(1,2)[0].value!=='mutated');
