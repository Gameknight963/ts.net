export {};
const cases: number[][] = [
 [1000000,0],[-1000000,0],[1e100,0],[-1e100,0],
 [2000,1e100],[2000,-1e100],[2000,0,1e100],[2000,0,-1e100],
 [2000,0,1,1e100],[2000,0,1,-1e100],[2000,0,1,0,1e100],
 [2000,0,1,0,0,1e100],[2000,0,1,0,0,0,1e100],
 [NaN,0],[Infinity,0],[2000,Infinity],
];
for(const values of cases) {
 const [year,month,day=1,hour=0,minute=0,second=0,millisecond=0]=values;
 const date=new Date(year,month,day,hour,minute,second,millisecond);
 console.log('construct',Number.isNaN(date.getTime()),date.toString(),(date as any).toJSON());
}
for(const method of ['setFullYear','setMonth','setDate','setHours','setMinutes','setSeconds','setMilliseconds','setYear']) {
 const date=new Date(0);const result=(date as any)[method](1e100);
 console.log(method,Number.isNaN(result),Number.isNaN(date.getTime()));
}
