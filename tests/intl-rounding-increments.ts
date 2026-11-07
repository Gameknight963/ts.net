const increments = [1,2,5,10,20,25,50,100,200,250,500,1000,2000,2500,5000];
for (const roundingIncrement of increments) {
 const options:any={minimumFractionDigits:2,maximumFractionDigits:2,roundingIncrement};
 const formatter=new Intl.NumberFormat('en-US',options);
 for (const value of [-12.375,-1.025,-0.001,-0,0,0.001,1.025,12.375,101.25]) console.log(roundingIncrement,formatter.format(value),value.toLocaleString('en-US',options));
 console.log('increment',(formatter.resolvedOptions() as any).roundingIncrement);
}
for (const roundingMode of ['ceil','floor','expand','trunc','halfCeil','halfFloor','halfExpand','halfTrunc','halfEven']) {
 const formatter=new Intl.NumberFormat('en-US',{minimumFractionDigits:2,maximumFractionDigits:2,roundingIncrement:25,roundingMode} as any);
 console.log(roundingMode,[-1.375,-1.125,-1.1,1.1,1.125,1.375].map(value=>formatter.format(value)).join('|'));
}
for(const style of ['decimal','percent','currency']) {
 const formatter=new Intl.NumberFormat('en-US',{style,currency:'USD',minimumFractionDigits:0,maximumFractionDigits:0,roundingIncrement:500} as any);
 console.log(style,[-1001n,-1n,0n,1n,1001n,123456789012345678901n].map(value=>formatter.format(value).replace(/[ \u00a0\u202f]/g,'')).join('|'));
}
const currency=new Intl.NumberFormat('en-US',{style:'currency',currency:'USD',roundingIncrement:5} as any);console.log(currency.format(1.025));
for(const roundingIncrement of [0,3,6,5001,NaN,Infinity])try{new Intl.NumberFormat('en',{roundingIncrement} as any);}catch(error){console.log('invalid',error instanceof RangeError);}
try{new Intl.NumberFormat('en',{roundingIncrement:5} as any);}catch(error){console.log('precision',error instanceof RangeError);}
let reads=0;const options:any={minimumFractionDigits:2,maximumFractionDigits:2,get roundingIncrement(){reads++;return 5;}};const formatter=new Intl.NumberFormat('en',options);formatter.format(1.025);formatter.format(2.025);console.log(reads);
