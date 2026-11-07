const date = new Date(Date.UTC(2024, 1, 29, 12, 34, 56));
console.log("utc", date.toLocaleString("en-GB", {timeZone:"UTC"}).replace(",", ""));
console.log("new-york", date.toLocaleTimeString("en-GB", {timeZone:"America/New_York"}));
console.log("tokyo", date.toLocaleTimeString("en-GB", {timeZone:"Asia/Tokyo"}));
console.log("short-time", date.toLocaleTimeString("en-GB", {timeZone:"UTC",timeStyle:"short"}));
console.log("medium-time", date.toLocaleTimeString("en-GB", {timeZone:"UTC",timeStyle:"medium"}));
for (const style of ["short", "medium", "long", "full"] as const) {
 console.log("date-style", style, date.toLocaleDateString("en-US", {timeZone:"UTC", dateStyle:style}).includes("24"));
 console.log("time-style", style, date.toLocaleTimeString("en-GB", {timeZone:"UTC", timeStyle:style}).startsWith("12:34"));
}
for (const opts of [{dateStyle:"invalid"}, {timeZone:"No/Such_Zone"}]) {
 try { date.toLocaleString("en-GB", opts as Intl.DateTimeFormatOptions); } catch(e) { console.log("invalid", (e as Error).name); }
}
for (const method of [Date.prototype.toLocaleDateString,Date.prototype.toLocaleTimeString]) {
 try { method.call(date,"en-GB", {dateStyle:"short",timeStyle:"short"}); } catch(e) { console.log("wrong-method",(e as Error).name); }
}
console.log("invalid-date",new Date(NaN).toLocaleString("en-GB",{timeZone:"No/Such_Zone"}));


const summer = new Date(Date.UTC(2024,6,1,12,34,56));
console.log("daylight-saving", summer.toLocaleTimeString("en-GB", {timeZone:"America/New_York",timeStyle:"short"}));
console.log("combined-styles",date.toLocaleString("en-GB",{timeZone:"UTC",dateStyle:"short",timeStyle:"short"}).replace(",", ""));
console.log("empty-options",date.toLocaleString("en-GB",{}).replace(",", "")===date.toLocaleString("en-GB").replace(",", ""));
