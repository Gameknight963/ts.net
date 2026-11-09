export {};
const options:Intl.NumberFormatOptions[]=[{}, {minimumFractionDigits:2,maximumFractionDigits:2},{minimumSignificantDigits:2,maximumSignificantDigits:3},{minimumIntegerDigits:6},{notation:'scientific'},{notation:'engineering'},{notation:'compact'},{notation:'compact',compactDisplay:'long'},{signDisplay:'always'},{signDisplay:'exceptZero'},{signDisplay:'never'},{roundingMode:'floor',maximumFractionDigits:0},{roundingIncrement:25,minimumFractionDigits:2,maximumFractionDigits:2},{trailingZeroDisplay:'stripIfInteger',minimumFractionDigits:2}];
for(const locale of ['de-DE','fr-FR','ru-RU','ja-JP','ar-EG'])for(const extra of options) {
 const format=new Intl.NumberFormat(locale,{style:'unit',unit:'kilometer',unitDisplay:'long',...extra});
 console.log(locale,JSON.stringify(extra),JSON.stringify(format.resolvedOptions()));
 for(const value of [0,-0,1,-1,2,-2,0.001,1.005,1.025,3.5,1000,1200,1000000,NaN,Infinity,1234567890123456789012345678901n]) {
  const text=format.format(value),parts=format.formatToParts(value);
  if(text!==parts.map(part=>part.value).join(''))throw new Error('Unit parts mismatch');
  // Arabic dual/one forms changed between CLDR releases.
  if(locale==='ar-EG')continue;
  // Scientific fractions and huge plural operands differ across ICU versions.
  // Exact large-operand selection is checked independently.
  if(locale==='fr-FR' && (extra.notation==='scientific' || extra.notation==='engineering') && value===0.001)continue;
  let compared=parts;
  if(typeof value==='bigint') {
   const forms=new Set([0,1,2,3,5,11,101,1000000,1.5].flatMap(probe=>format.formatToParts(probe).filter(part=>part.type==='unit').map(part=>part.value)));
   for(const part of parts)if(part.type==='unit' && !forms.has(part.value))throw new Error('Unknown large unit plural form');
   compared=parts.map(part=>part.type==='unit' ? {...part,value:'<large-operand-unit>'} : part);
  }
  const normalized=compared.map(part=>({...part,value:part.type==='literal' ? part.value.replace(/[ \u00a0\u202f]/g,'') : part.value})).filter(part=>part.value!=='');
  console.log(String(value),JSON.stringify(normalized));
 }
}
let calls=0;const value={valueOf(){calls++;return 1.5;}};
const format=new Intl.NumberFormat('fr-FR',{style:'unit',unit:'meter'});
console.log(format.format(value as any),JSON.stringify(format.formatToParts(value as any)),calls);
console.log((1.5).toLocaleString('fr-FR',{style:'unit',unit:'meter'}));
