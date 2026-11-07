const modes = ['ceil','floor','expand','trunc','halfCeil','halfFloor','halfExpand','halfTrunc','halfEven'];
for (const mode of modes) {
 const options:any = {maximumFractionDigits:1,roundingMode:mode};
 const formatter = new Intl.NumberFormat('en-US', options);
 for (const value of [-2.01,-1.35,-1.25,-0.05,-0,0,0.05,1.25,1.35,2.01,NaN,Infinity,-Infinity]) console.log(mode,formatter.format(value),value.toLocaleString('en-US',options));
 console.log('mode', (formatter.resolvedOptions() as any).roundingMode);
}
for (const style of ['decimal','percent','currency']) {
 const options:any = {style,currency:'USD',minimumFractionDigits:2,maximumFractionDigits:2,trailingZeroDisplay:'stripIfInteger'};
 const formatter = new Intl.NumberFormat('en-US', options);
 for (const value of [0,-0,1,1.001,1.125,12n]) console.log(style,formatter.format(value).replace(/[ \u00a0\u202f]/g,''));
 console.log('zeros',(formatter.resolvedOptions() as any).trailingZeroDisplay);
}
for (const roundingMode of ['wrong', null]) try {new Intl.NumberFormat('en-US',{roundingMode} as any);} catch(error) {console.log('invalid mode', error instanceof RangeError);}
try {new Intl.NumberFormat('en-US',{trailingZeroDisplay:'wrong'} as any);} catch(error) {console.log('invalid zeros',error instanceof RangeError);}
let reads=0;const options:any={get roundingMode(){reads++;return 'halfEven';}};const formatter=new Intl.NumberFormat('en-US',options);formatter.format(1.25);formatter.format(1.35);console.log('reads',reads);
