for(const locale of ["en", "en-US", "tr", "tr-TR", "az"]){const text=locale.startsWith("en")?"iI\u0131":"iI\u0130\u0131";console.log(locale,text.toLocaleLowerCase(locale),text.toLocaleUpperCase(locale));}
console.log("list","I".toLocaleLowerCase(["tr","en"]),"i".toLocaleUpperCase(["tr","en"]));
console.log("default","AbC".toLocaleLowerCase(),"AbC".toLocaleUpperCase([]));
const lower:any=String.prototype.toLocaleLowerCase,upper:any=String.prototype.toLocaleUpperCase;
console.log("generic",lower.call(123,"tr"),upper.call({toString(){return "i";}},"tr"));
for(const locale of ["", "en_US", ["tr", "en_US"], [1], null])try{lower.call("I",locale);}catch(error){console.log("invalid",(error as Error).name);}
let events="";const text={toString(){events+="text";return "I";}};const tag={toString(){events+="locale";return "tr";}};console.log("order",lower.call(text,[tag]),events);
console.log("metadata",lower.name,lower.length,upper.name,upper.length);
