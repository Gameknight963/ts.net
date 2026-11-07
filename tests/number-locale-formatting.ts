for(const locale of ["en-US","de-DE","en-IN"])for(const value of [0,-0,1,-1,1234567.125,-1234567.125,1.23456,NaN,Infinity,-Infinity])console.log(locale,value.toLocaleString(locale));
const value=12345.125;for(const options of [{minimumFractionDigits:2},{maximumFractionDigits:1},{minimumFractionDigits:4,maximumFractionDigits:5},{useGrouping:false},{minimumFractionDigits:0,maximumFractionDigits:0}])console.log("options",value.toLocaleString("en-US",options));
console.log("boxed",new Number(1234.5).toLocaleString("en-US"));
const format:any=Number.prototype.toLocaleString;console.log("list",format.call(1234.5,["de-DE","en-US"]));
for(const receiver of ["123",true,{},null,undefined])try{format.call(receiver,"en-US");}catch(error){console.log("receiver",(error as Error).name);}
for(const options of [{minimumFractionDigits:-1},{maximumFractionDigits:101},{minimumFractionDigits:4,maximumFractionDigits:1},{maximumFractionDigits:NaN}])try{format.call(1,"en-US",options);}catch(error){console.log("digits",(error as Error).name);}
console.log("metadata",format.name,format.length);
