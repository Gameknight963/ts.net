for(const [minimumSignificantDigits,maximumSignificantDigits] of [[1,1],[1,3],[3,3],[5,7],[21,21]]) {
 const options={minimumSignificantDigits,maximumSignificantDigits}; const format=new Intl.NumberFormat('en-US',options);
 for(const value of [0,-0,1,1.25,-1.25,9.999,99.99,123456,0.0012345,1e25,5e-324,NaN,Infinity,-Infinity]) console.log(minimumSignificantDigits,maximumSignificantDigits,format.format(value),value.toLocaleString('en-US',options));
 for(const value of [0n,1n,-1n,999999n,123456789012345678901234567890n])console.log('big',format.format(value));
 console.log(JSON.stringify(format.resolvedOptions()));
}
for(const roundingMode of ['ceil','floor','halfEven','halfExpand']) {
 const formatter=new Intl.NumberFormat('en',{maximumSignificantDigits:2,roundingMode} as any);
 console.log(roundingMode,[-9.99,-1.25,1.25,9.99].map(value=>formatter.format(value)).join('|'));
}
for(const style of ['decimal','percent','currency']) {
 const format=new Intl.NumberFormat('en-US',{style,currency:'USD',minimumSignificantDigits:3,maximumSignificantDigits:3} as any);
 for(const value of [0.001234,1,9.999,1234,123456789n])console.log(style,format.format(value).replace(/[ \u00a0\u202f]/g,''));
}
const parts=new Intl.NumberFormat('en',{minimumSignificantDigits:5,maximumSignificantDigits:5}).formatToParts(1.2);console.log(parts.map(part=>part.type+':'+part.value).join('|'));
const ignored=new Intl.NumberFormat('en',{minimumFractionDigits:7,maximumFractionDigits:2,maximumSignificantDigits:3});console.log(ignored.format(12.345));
for(const options of [{minimumSignificantDigits:0},{maximumSignificantDigits:22},{minimumSignificantDigits:5,maximumSignificantDigits:3}])try{new Intl.NumberFormat('en',options);}catch(error){console.log('invalid',error instanceof RangeError);}
try {new Intl.NumberFormat('en',{maximumSignificantDigits:3,roundingIncrement:5} as any);}catch(error){console.log('increment',error instanceof TypeError);}
let reads=0;const format=new Intl.NumberFormat('en',{get maximumSignificantDigits(){reads++;return 3;}});format.format(1);format.format(2);console.log('reads',reads);
