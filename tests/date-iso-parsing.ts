for (const text of ['0000','0000-02','0000-02-29','-000001-01-01T00:00:00.000Z','+010000-01-01T00:00:00.000Z','-271821-04-20T00:00:00.000Z','+275760-09-13T00:00:00.000Z','+275760-09-13T01:00:00.000+01:00','-271821-04-19T23:00:00.000-01:00','+010000-02-29T24:00:00Z','2000-02-30','2000-01-01T00:00:00.1Z','2000-01-01T00:00:00.123456Z','2000-01-01T00:00:00-0530','2000-01-01T12:34:56','2000-01-01','2000-01']) {
    const value = Date.parse(text);
    console.log(text,value,Number.isNaN(value) ? 'invalid' : new Date(text).toISOString());
}
for (const text of ['-000000-01-01T00:00:00Z','+275760-09-13T00:00:00.001Z','-271821-04-19T23:59:59.999Z','2000-13-01','2000-01-32','2000-01-01T24:00:01Z','2000-01-01T24:00:00.0001Z','2000-01-01T12:60:00Z','2000-01-01T12:00:60Z','2000-01-01T12:00:00+24:00']) console.log('invalid',text,Number.isNaN(Date.parse(text)));
let seed = 8128;
for(let i=0;i<100;i++) {
    seed=(Math.imul(seed,1664525)+1013904223)>>>0;
    const timestamp=Math.trunc((seed/4294967296*2-1)*8640000000000000);
    const iso=new Date(timestamp).toISOString();
    console.log('roundtrip',Date.parse(iso)===timestamp,new Date(iso).getTime()===timestamp);
}
