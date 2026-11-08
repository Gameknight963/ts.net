console.log('alias', (Date.prototype as any).toGMTString === Date.prototype.toUTCString);
for (const timestamp of [-8640000000000000,-62198755200000,-62167219200000,-62135596800001,-1,0,253402300800000,8640000000000000,NaN]) {
    const date = new Date(timestamp);
    console.log(timestamp,date.toUTCString(),(date as any).toGMTString());
}
let seed = 8191;
for(let index=0;index<100;index++) {
    seed=(Math.imul(seed,1664525)+1013904223)>>>0;
    const timestamp=Math.trunc((seed/4294967296*2-1)*8640000000000000);
    console.log(new Date(timestamp).toUTCString());
}
console.log('metadata',Date.prototype.toUTCString.name,Date.prototype.toUTCString.length);
try { Date.prototype.toUTCString.call({}); }
catch(error) { console.log('receiver',(error as Error).name); }
