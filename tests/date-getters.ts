const names=["getFullYear","getMonth","getDate","getDay","getHours","getMinutes","getSeconds","getMilliseconds","getUTCFullYear","getUTCMonth","getUTCDate","getUTCDay","getUTCHours","getUTCMinutes","getUTCSeconds","getUTCMilliseconds","getTimezoneOffset","getYear"];
for(const value of [0,-1,1,951827696789,1710054000000,1730613600000,1893456000000,NaN]){const date:any=new Date(value);console.log("date",value,names.map(name=>date[name]()).join("|"));}
for(const name of names){const method:any=(Date.prototype as any)[name];console.log("metadata",method.name,method.length);try{method.call({});}catch(error){console.log("invalid",(error as Error).name);}}
const date=new Date(951827696789);console.log("typed",date.getUTCFullYear(),date.getUTCMonth(),date.getUTCDate(),date.getUTCHours(),date.getUTCMinutes(),date.getUTCSeconds(),date.getUTCMilliseconds());
console.log("keys",Object.keys(Date.prototype).length);
