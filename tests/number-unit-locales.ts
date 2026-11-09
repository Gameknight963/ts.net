export {};
for(const locale of ['en-GB','de-DE','fr-FR','ru-RU','pl-PL','ja-JP','ar-EG','hi-IN'])for(const unit of ['meter','kilogram','celsius','percent','mile-per-hour','liter-per-kilometer'])for(const unitDisplay of ['long','short','narrow'] as const) {
 // These forms changed in newer CLDR data; exercise their text/parts
 // consistency below rather than asserting one release's translated spelling.
 const dataVariation=unit==='celsius' && (unitDisplay==='narrow' && ['en-GB','de-DE','pl-PL'].includes(locale) || locale==='pl-PL' && unitDisplay==='short')
  || unit==='mile-per-hour' && unitDisplay!=='long' && ['ru-RU','pl-PL'].includes(locale)
  || locale==='hi-IN' && unitDisplay==='narrow' && ['kilogram','mile-per-hour'].includes(unit)
  || locale==='ar-EG' && (unit==='meter' && unitDisplay==='long' || unit==='liter-per-kilometer' && unitDisplay!=='short');
 const format=new Intl.NumberFormat(locale,{style:'unit',unit,unitDisplay});
 for(const value of [0,-0,1,-1,2,3,5,11,21,1.5,1000,NaN,Infinity,-Infinity]) {
  const text=format.format(value),parts=format.formatToParts(value);
  if(parts.map(part=>part.value).join('')!==text)throw new Error('Unit parts differ from text');
  if(dataVariation)continue;
  const normalized=parts.map(part=>({...part,value:part.type==='literal' ? part.value.replace(/[ \u00a0\u202f]/g,'') : part.value})).filter(part=>part.value!=='');
  console.log(locale,unit,unitDisplay,String(value),JSON.stringify(normalized));
 }
}

// Every sanctioned simple unit, including compound denominators, must format.
for(const locale of ['de-DE','fr-FR','ru-RU','ja-JP','ar-EG','hi-IN'])for(const unit of Intl.supportedValuesOf('unit'))for(const unitDisplay of ['long','short','narrow'] as const) {
 for(const selected of [unit,unit+'-per-second']) {
  const format=new Intl.NumberFormat(locale,{style:'unit',unit:selected,unitDisplay});
  for(const value of [0,1,2,3,1.5,-1,NaN,Infinity,123456789012345678901234567890n]) {
   const text=format.format(value),parts=format.formatToParts(value);
   if(parts.map(part=>part.value).join('')!==text || !parts.some(part=>part.type==='unit' && part.value.length>0))throw new Error('Missing or inconsistent unit');
  }
 }
 console.log('covered',locale,unit,unitDisplay);
}
