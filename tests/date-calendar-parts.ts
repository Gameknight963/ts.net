for(const parts of [[2024,0,1],[2024,1,29,12,34,56,789],[2024,12,1],[2024,-1,1],[2024,0,0],[2024,0,32],[2024,0,1,25,-1,61,1001],[0,0,1],[99,0,1],[2024.9,1.9,2.9],[2024,NaN,1],[2024,0,1,Infinity]]){const date=new Date(...parts as [number,number,number,number,number,number,number]);const value=Date.UTC(...parts as [number,number,number,number,number,number,number]);console.log(parts.join("|"),date.getTime(),value,(date.toJSON as any).call(date));}
console.log("defaults",Date.UTC(2024),(Date.UTC as any)(),Date.UTC(undefined as any),Date.UTC(2024,undefined as any));
console.log("dst",new Date(2024,2,10,2,30).getTime(),new Date(2024,10,3,1,30).getTime());
let events="";const year={valueOf(){events+="year";return NaN;}};const month={valueOf(){events+="month";return 0;}};console.log("conversion",Date.UTC(year as any,month as any),events);
class Moment extends Date{}const derived=new Moment(2024,0,1);console.log("derived",derived instanceof Moment,derived.getFullYear(),derived.getMonth(),derived.getDate());
console.log("early UTC",Date.UTC(100,0,1));
console.log("metadata",Date.UTC.name,Date.UTC.length);
