const date=new Date(Date.UTC(2024,1,29,12,34,56));
console.log("date",date.toLocaleDateString("en-US"));console.log("time",date.toLocaleTimeString("en-US"));
// Native .NET's combined format omits the comma used by Intl in en-US.
console.log("combined",date.toLocaleString("en-US").replace(",",""));
console.log("list",date.toLocaleDateString(["en-US","de"])===date.toLocaleDateString("en-US"));
console.log("cultures",date.toLocaleDateString("de")!==date.toLocaleDateString("en-US"),date.toLocaleString("de").length>0);
for(const method of [Date.prototype.toLocaleString,Date.prototype.toLocaleDateString,Date.prototype.toLocaleTimeString]){console.log("metadata",method.name,method.length);console.log("invalid",method.call(new Date(NaN),"en_US"));try{method.call({});}catch(error){console.log("receiver",(error as Error).name);}}
try{date.toLocaleString("en_US");}catch(error){console.log("locale",(error as Error).name);}
