for (const year of [-271821,-10000,-400,-100,-1,0,1,99,100,400,1900,2000,10000,275760]) {
    for (const month of [-13,0,1,11,24]) {
        const timestamp = Date.UTC(year,month,29,25,-60,61,1001);
        console.log('utc',year,month,timestamp,Number.isNaN(timestamp) ? 'invalid' : new Date(timestamp).toISOString());
    }
}
for (const timestamp of [-8640000000000000,-62167219200000,253402300800000,8640000000000000,NaN]) {
    for (const field of ['FullYear','Month','Date','Hours','Minutes','Seconds','Milliseconds']) {
        const date = new Date(timestamp);
        const result = (date as any)['setUTC'+field](field === 'FullYear' ? 10000 : 1);
        console.log('set',timestamp,field,result,Number.isNaN(result) ? 'invalid' : date.toISOString());
    }
}
const zero = new Date(0);
zero.setUTCFullYear(0,1,29);
console.log('year zero leap',zero.toISOString());
console.log('maximum',Date.UTC(275760,8,13),Date.UTC(275760,8,13,0,0,0,1));
console.log('minimum',Date.UTC(-271821,3,20),Date.UTC(-271821,3,19,23,59,59,999));
for (const value of [NaN,Infinity,-Infinity,1e100]) console.log('invalid',Date.UTC(value),Date.UTC(2000,value));
