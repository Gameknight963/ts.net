const cases:any[]=[["setFullYear",2025,1,28],["setMonth",13,2],["setDate",0],["setHours",25,-1,61,1001],["setMinutes",61,-1,1001],["setSeconds",61,-1],["setMilliseconds",-1]];
for(const utc of [false,true])for(const item of cases){const date:any=new Date(Date.UTC(2024,0,31,12,34,56,789));const name=utc?item[0].replace("set","setUTC"):item[0];const result=date[name](...item.slice(1));console.log(name,result,date.toISOString(),(Date.prototype as any)[name].length);}
for(const name of ["setUTCFullYear","setFullYear","setUTCMonth","setDate","setHours","setSeconds","setMilliseconds"]){const date:any=new Date(NaN);console.log("invalid",name,date[name](2024),(date.toJSON as any).call(date));}
const year=new Date(0);year.setUTCFullYear(99,0,1);console.log("year",year.toISOString());
const legacy:any=new Date(NaN);console.log("legacy",legacy.setYear(99),legacy.getFullYear(),legacy.getMonth(),legacy.getDate());
const missing:any=new Date(0);console.log("missing",missing.setUTCMonth(),missing.getTime());
const explicit=new Date(0);console.log("undefined",explicit.setUTCHours(1,undefined as any),explicit.getTime());
let events="";const original=new Date(Date.UTC(2024,0,31));const month={valueOf(){events+="month";original.setTime(0);return 1;}};console.log("snapshot",original.setUTCMonth(month as any),original.toISOString(),events);
const invalid=new Date(NaN);const value={valueOf(){events+="value";return 1;}};invalid.setUTCMonth(value as any);console.log("conversion",events);
const dst=new Date(2024,2,10,1,30);dst.setHours(2);console.log("dst",dst.getTime());
