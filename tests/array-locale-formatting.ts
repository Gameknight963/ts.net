console.log([1234.5,67.25].toLocaleString("en-US"),[1234.5,67.25].toLocaleString("de-DE"));
console.log([1,2].toLocaleString("en-US",{minimumFractionDigits:2}));
const mixed:any[]=[null,undefined,,true,"hello",[1234.5,null],{}];console.log(mixed.toLocaleString("en-US"));
const options={maximumFractionDigits:1};const item:any={toLocaleString(locale:any,received:any){console.log("callback",locale,received===options,arguments.length);return "item";}};console.log([item,item].toLocaleString("en-US",options));
const format:any=Array.prototype.toLocaleString;console.log("generic",format.call({0:1234.5,2:"x",length:4},"en-US"),format.call("ab","en-US"));
const inherited:any=Object.create({1:1000});inherited.length=2;console.log("inherited",format.call(inherited,"en-US"));
const cyclic:any[]=[1];cyclic.push(cyclic,2);console.log("cycle",cyclic.toLocaleString("en-US"));
try{[{toLocaleString:1} as any].toLocaleString();}catch(error){console.log("invalid",(error as Error).name);}
let events="";const values:any[]=[{toLocaleString(){events+="first";values[1]=2000;values.push(3);return "first";}},1];console.log("mutation",values.toLocaleString("en-US"),events,values.length);
console.log("metadata",format.name,format.length);
