export {};
for(const locale of ["en-US","de-DE","hi-IN"])for(const minimumIntegerDigits of [1,2,5,10,21])for(const useGrouping of [false,true]) {
 for(const value of [-1234.5,-0,0,0.125,12,123456.75,Infinity,NaN]) console.log(locale,minimumIntegerDigits,useGrouping,String(value),value.toLocaleString(locale,{minimumIntegerDigits,useGrouping}));
 for(const value of [-1234n,0n,12n,1234567890123456789012345n]) console.log(locale,minimumIntegerDigits,useGrouping,String(value),value.toLocaleString(locale,{minimumIntegerDigits,useGrouping} as any));
}
for(const minimumIntegerDigits of [0,22,NaN,Infinity])try{console.log((1).toLocaleString("en",{minimumIntegerDigits}));}catch(error){console.log("invalid",(error as Error).name);}
console.log("combined",(-0.001).toLocaleString("en",{minimumIntegerDigits:5,maximumFractionDigits:2,signDisplay:"exceptZero"}));
console.log("coerce",(12).toLocaleString("en",{minimumIntegerDigits:"5" as any,useGrouping:false}));
