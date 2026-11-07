const date = new Date(Date.UTC(2024,1,29,13,4,5,678));
for (const locale of ["en-US","en-GB"]) {
 for (const fields of [
  {hour:"numeric"},{hour:"2-digit"},{minute:"numeric"},{second:"numeric"},
  {hour:"numeric",minute:"numeric"},{hour:"2-digit",minute:"2-digit",second:"2-digit"},
  {second:"numeric",fractionalSecondDigits:3},{fractionalSecondDigits:2},
  {hour:"numeric",minute:"numeric",hour12:true},{hour:"2-digit",minute:"2-digit",hour12:false}
 ] as Intl.DateTimeFormatOptions[]) console.log(locale,JSON.stringify(fields),date.toLocaleTimeString(locale,{...fields,timeZone:"UTC"}));
}
for(const cycle of ["h11","h12","h23","h24"] as const) {
 for (const timestamp of [Date.UTC(2024,1,29,0,4),Date.UTC(2024,1,29,12,4)])
  console.log(cycle,new Date(timestamp).toLocaleTimeString("en-US",{timeZone:"UTC",hourCycle:cycle,hour:"2-digit",minute:"2-digit"}));
}
for (const options of [{hour:"invalid"},{hourCycle:"invalid"},{fractionalSecondDigits:0},{fractionalSecondDigits:4},{fractionalSecondDigits:NaN},{timeZoneName:"invalid"},{timeStyle:"short",hour:"numeric"}]) {
 try {date.toLocaleTimeString("en-US",options as Intl.DateTimeFormatOptions);} catch(error) { console.log("invalid",(error as Error).name); }
}
console.log("combined",date.toLocaleString("en-GB",{year:"numeric",month:"2-digit",day:"2-digit",hour:"2-digit",minute:"2-digit",timeZone:"UTC"}).replace(",",""));
console.log("date-method",date.toLocaleDateString("en-GB",{hour:"2-digit",minute:"2-digit",timeZone:"UTC"}).replace(",",""));
console.log("clock-only",date.toLocaleTimeString("en-GB",{hour12:false,timeZone:"UTC"}));
for (const zone of ["UTC","Asia/Kolkata","America/New_York"]) {
 for(const name of ["shortOffset","longOffset"] as const)
  console.log(zone,name,date.toLocaleTimeString("en-GB",{hour:"2-digit",minute:"2-digit",timeZone:zone,timeZoneName:name}));
}

for (const hour12 of [true,false]) console.log("styled-clock",date.toLocaleTimeString("en-GB",{timeZone:"UTC",timeStyle:"short",hour12}));

for (const digits of [1,2,3] as const) console.log("precision",digits,date.toLocaleTimeString("en-GB",{timeZone:"UTC",hour:"2-digit",minute:"2-digit",second:"2-digit",fractionalSecondDigits:digits}));
console.log("precedence",new Date(Date.UTC(2024,1,29,0)).toLocaleTimeString("en-US",{timeZone:"UTC",hour:"numeric",hour12:true,hourCycle:"h24"}));
const summer = new Date(Date.UTC(2024,6,1,12));
console.log("summer-offset",summer.toLocaleTimeString("en-GB",{timeZone:"America/New_York",hour:"2-digit",minute:"2-digit",timeZoneName:"longOffset"}));
console.log("truncated-digits",date.toLocaleTimeString("en-GB",{timeZone:"UTC",second:"numeric",fractionalSecondDigits:2.9 as 2}));
