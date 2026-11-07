const tags=["en-US-u-ca-japanese","de-DE-u-co-phonebk-kf-upper-kn","en-u-kn-false-nu-arab-hc-h23","fr-u-kn-true","en-u-attr-ca-gregory-zz-foobar","en-t-de-u-ca-gregory-x-private","en-u-ca-co-nu","en-u-hc-foo-kf-foo-kn-foo"];
for(const tag of tags){const value=new Intl.Locale(tag);console.log(value.toString(),value.baseName,value.language,value.region,value.calendar,value.collation,value.hourCycle,value.caseFirst,value.numeric,value.numberingSystem);console.log(new Intl.Locale(value).toString());}
for(const options of [{calendar:"japanese"},{collation:"phonebk",caseFirst:"upper" as const,numeric:true},{hourCycle:"h24" as const,numberingSystem:"arab"},{numeric:false},{calendar:"GREGORY"},{language:"fr",region:"FR",calendar:"buddhist"}]){
 const value=new Intl.Locale("en-US-u-ca-japanese-kn",options);console.log(value.toString(),value.baseName,value.calendar,value.collation,value.hourCycle,value.caseFirst,value.numeric,value.numberingSystem);
}
for(const options of [{calendar:"bad_type"},{collation:"a"},{hourCycle:"h25"},{caseFirst:"UPPER"},{numberingSystem:""}])try{new Intl.Locale("en",options as any);}catch(error){console.log("invalid",(error as Error).name);}
let reads=0;const options={get numeric(){reads++;return true;}};const value=new Intl.Locale("en",options);console.log("getter",value.numeric,reads);console.log(value.numeric,reads);
const preserved=new Intl.Locale("en-u-attr-zz-foobar-kn-false",{numeric:true,calendar:"gregory"});console.log("preserved",preserved.toString(),preserved.baseName);
const full=new Intl.Locale("de-DE-u-co-phonebk-kn");console.log("canonical",Intl.getCanonicalLocales([full as any]).join("|"));
(full as any).toString=()=>"fr";console.log("internal-copy",new Intl.Locale(full).toString());
let events="";const source={toString(){events+="tag|";return "en-u-kn";}};const observedOptions={get language(){events+="language|";return "fr";},get numeric(){events+="numeric|";return false;}};console.log("order",new Intl.Locale(source as any,observedOptions).toString(),events);
console.log("canonical-keywords",Intl.getCanonicalLocales(["de-DE-u-kn-true-co-phonebk","de-DE-u-co-phonebk-kn"]).join("|"));
