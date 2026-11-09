export {};
function text(value:string):string{return value.replace(/ \([^)]*\)$/,'');}
for(const timestamp of [-8640000000000000,8640000000000000,-62167219200000,-62135596800001,-62135596800000,253402300799999,253402300800000]) {
    const date=new Date(timestamp);
    console.log('local',timestamp,date.getFullYear(),date.getMonth(),date.getDate(),date.getDay(),date.getHours(),date.getMinutes(),date.getSeconds(),date.getMilliseconds(),date.getTimezoneOffset(),(date as any).getYear());
    // Friendly timezone names can differ between the platform and Node.
    console.log('text',date.toDateString(),text(date.toTimeString()),text(date.toString()));
}
let seed=67891;
for(let i=0;i<100;i++) {
    seed=(Math.imul(seed,1664525)+1013904223)>>>0;
    const timestamp=Math.trunc((seed/4294967296*2-1)*8640000000000000);
    const date=new Date(timestamp);
    console.log('random',timestamp,date.getFullYear(),date.getMonth(),date.getDate(),date.getDay(),date.getHours(),date.getMinutes(),date.getSeconds(),date.getMilliseconds(),date.getTimezoneOffset());
}
for(const year of [-10000,-400,-1,0,10000,20000,275000]) {
    const date=new Date(2000,1,28,12,34,56,789);date.setFullYear(year);
    console.log('year',year,date.toISOString(),date.getFullYear(),date.getMonth(),date.getDate());
    const constructed=new Date(year===0 ? -400 : year,13,32,25,61,62,1001);
    console.log('construct',constructed.toISOString(),constructed.getFullYear());
    const parsed=new Date(date.toISOString().slice(0,-1));console.log('parse',parsed.getTime(),parsed.getFullYear());
    date.setMonth(2);date.setDate(0);date.setHours(1,2,3,4);date.setMinutes(61);date.setSeconds(62);date.setMilliseconds(1001);
    console.log('setters',date.toISOString(),date.getFullYear(),date.getMonth(),date.getDate(),date.getHours(),date.getMinutes(),date.getSeconds(),date.getMilliseconds());
}
