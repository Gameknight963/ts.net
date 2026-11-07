for(const tag of ["und","en","EN-us","zh-Hant-TW","sr-Cyrl-RS","de-DE","es-419"]){const locale=new Intl.Locale(tag);console.log(locale.toString(),locale.baseName,locale.language,locale.script,locale.region,locale.numeric);console.log(String(locale),Object.prototype.toString.call(locale));}
for(const options of [{language:"fr"},{script:"Latn"},{region:"GB"},{language:"sr",script:"Cyrl",region:"RS"}]){const locale=new Intl.Locale("en-US",options);console.log(locale.toString(),locale.language,locale.script,locale.region);}
const original=new Intl.Locale("zh-Hant-TW");console.log("copy",new Intl.Locale(original).toString(),Intl.getCanonicalLocales([original as any]).join("|"));
class Regional extends Intl.Locale {label="regional";}const regional=new Regional("en",{region:"US"});console.log("derived",regional.toString(),regional.label,regional instanceof Regional,regional instanceof Intl.Locale);
for(const tag of [null,undefined,1,"","en_US"]){try{new Intl.Locale(tag as any);}catch(error){console.log("invalid",(error as Error).name);}}
for(const options of [{language:"x"},{script:"abc"},{region:"USA"},{region:"12"}])try{new Intl.Locale("en",options);}catch(error){console.log("option",(error as Error).name);}
const descriptor=Object.getOwnPropertyDescriptor(Intl.Locale.prototype,"language")!;try{descriptor.get!.call({});}catch(error){console.log("receiver",(error as Error).name);}
try{(Intl.Locale as any)("en");}catch(error){console.log("new",(error as Error).name);}
console.log("metadata",Intl.Locale.name,Intl.Locale.length,Intl.Locale.prototype.toString.name,Intl.Locale.prototype.toString.length);
