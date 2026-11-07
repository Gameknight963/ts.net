for(const locales of [undefined,[],"und","EN-us",["en-us","EN-US","fr-fr","zh-hant-tw"],["zz-ZZ"],{0:"DE-de",2:"ja-jp",length:3}])
 console.log(JSON.stringify(Intl.getCanonicalLocales(locales as any)));
const events:string[]=[];
const locales={get length(){events.push("length");return 3;},get 0(){events.push("0");return {toString(){events.push("string");return "en-us";}};},get 2(){events.push("2");return "fr-fr";}};
console.log(Intl.getCanonicalLocales(locales as any).join("|"),events.join("|"));
for(const locales of [null,[1],[undefined],[null],"", "en_US",["en","en_US"]]) {
 try{Intl.getCanonicalLocales(locales as any);}catch(error){console.log("invalid",(error as Error).name);}
}
console.log(Intl.getCanonicalLocales.name,Intl.getCanonicalLocales.length);
const first=Intl.getCanonicalLocales("en");first.push("fr");console.log(Intl.getCanonicalLocales("en").join("|"));
