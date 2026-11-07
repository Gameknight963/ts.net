export {};
for (const locale of ["en-US","de-DE"]) for (const signDisplay of ["auto","always","exceptZero","never","negative"] as const) {
 for (const value of [-1234.5,-0.0001,-0,0,0.0001,1234.5,NaN,Infinity,-Infinity]) console.log(locale,signDisplay,String(value),value.toLocaleString(locale,{signDisplay} as any));
 for (const value of [-1234n,0n,1234n]) console.log(locale,signDisplay,String(value),value.toLocaleString(locale,{signDisplay} as any));
}
for (const value of ["bad",null]) try {console.log((1).toLocaleString("en",{signDisplay:value as any}));} catch(error) { console.log("invalid",(error as Error).name); }
