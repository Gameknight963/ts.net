for(const locale of ["en", "de", "sv", "tr"])for(const pair of [["a","b"],["b","a"],["same","same"],["a","A"],["\u00e4","z"],["2","10"]])console.log(locale,pair.join("|"),Math.sign(pair[0].localeCompare(pair[1],locale)));
const compare:any=String.prototype.localeCompare;console.log("generic",Math.sign(compare.call(12,13,"en")),compare.call(true,true,"en"));
console.log("list",Math.sign("a".localeCompare("b",["en","de"])));
const values=["c","a","b"];values.sort((a,b)=>a.localeCompare(b,"en"));console.log("sort",values.join("|"));
console.log("numeric call","123".localeCompare(123 as any));
let events="";const left={toString(){events+="left";return "a";}};const right={toString(){events+="right";return "b";}};console.log("conversion",Math.sign(compare.call(left,right,"en")),events);
for(const locale of ["", "en_US"])try{compare.call("a","b",locale);}catch(error){console.log("locale",(error as Error).name);}
for(const value of [null,undefined])try{compare.call(value,"b");}catch(error){console.log("invalid",(error as Error).name);}
console.log("metadata",compare.name,compare.length);
