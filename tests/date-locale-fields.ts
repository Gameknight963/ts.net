// CLR culture data abbreviates German February as Feb rather than Feb.
const value = new Date(Date.UTC(2024,1,29,12,34,56));
for (const locale of ["en-US", "en-GB", "de-DE"]) {
 for (const options of [
  {year:"numeric",month:"long",day:"numeric"},
  {year:"numeric",month:"short",day:"2-digit"},
  {year:"2-digit",month:"2-digit",day:"2-digit"},
  {year:"numeric"}, {month:"long"}, {day:"numeric"},
  {year:"numeric",month:"long"},
  {weekday:"long",year:"numeric",month:"long",day:"numeric"},
  {weekday:"long"}, {weekday:"short"}, {weekday:"narrow"}
 ] as Intl.DateTimeFormatOptions[]) console.log(locale, JSON.stringify(options),value.toLocaleDateString(locale,{...options,timeZone:"UTC"}).replace("Feb.","Feb"));
}
for (const options of [{month:"invalid"},{weekday:"invalid"},{year:"long"},{dateStyle:"long",month:"long"}]) {
 try {value.toLocaleDateString("en-US",options as Intl.DateTimeFormatOptions);} catch(error) {console.log("invalid",(error as Error).name);}
}

console.log("narrow-month",value.toLocaleDateString("en-US",{month:"narrow",timeZone:"UTC"}));
console.log("month-day",value.toLocaleDateString("en-US",{month:"long",day:"numeric",timeZone:"UTC"}));
console.log("date-fields-on-time",value.toLocaleTimeString("en-GB",{year:"numeric",timeZone:"UTC"}).replace(",", ""));
