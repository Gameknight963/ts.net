for(const value of [0,1,-1,1234.9,-1234.9,NaN,Infinity,-Infinity,8640000000000001,null,undefined,true,false] as any[]){const date=new Date(value);console.log("time",date.getTime(),date.valueOf(),(date.toJSON as any).call(date));if(Number.isFinite(date.getTime()))console.log(date.toISOString());}
for(const text of ["1970-01-01T00:00:00.000Z","2024-02-29T12:34:56.789Z","2000-01-01","2000-01-01T12:00:00+02:00","not a date"]){const date=new Date(text);console.log("parse",date.getTime(),Date.parse(text),(date.toJSON as any).call(date));}
const date=new Date(0);console.log("utc",date.toUTCString(),(date as any).toGMTString(),Object.prototype.toString.call(date),Object.keys(date).length,date instanceof Date);
console.log("set",date.setTime(1234.9),date.toISOString());console.log("invalid",date.setTime(NaN),String(date),(date.toJSON as any).call(date));try{date.toISOString();}catch(error){console.log("iso error",(error as Error).name);}
const original=new Date(0);original.valueOf=()=>100;const copied=new Date(original);console.log("copy",copied.getTime());
console.log("json",JSON.stringify({date:new Date(0),invalid:new Date(NaN)}));
try{Date.prototype.getTime();}catch(error){console.log("prototype",(error as Error).name);}
for(const method of [Date.prototype.getTime,Date.prototype.setTime,Date.prototype.toISOString] as any[])try{method.call({});}catch(error){console.log("receiver",(error as Error).name);}
class Moment extends Date {marker=7;}const derived=new Moment(0);console.log("derived",derived instanceof Date,derived instanceof Moment,derived.marker,derived.toISOString());
const reflected=Reflect.construct(Date,[0],Moment);console.log("reflect",reflected instanceof Moment,reflected.getTime());
const now=Date.now();console.log("now",typeof now,Number.isFinite(now),Math.abs(new Date().getTime()-now)<10000,typeof Date());
console.log("generic",Date.prototype.toJSON.call({valueOf(){return 1;},toISOString(){return "custom";}} as any));
console.log("default",(new Date(0) as any)+""===new Date(0).toString(),Number(new Date(0)));
