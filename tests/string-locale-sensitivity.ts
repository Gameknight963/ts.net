for(const sensitivity of ["base","accent","case","variant"] as const)for(const pair of [["a","A"],["a","\u00e1"],["A","\u00e1"],["a","b"],["\u00e9","e\u0301"]])console.log(sensitivity,pair.join("|"),Math.sign(pair[0].localeCompare(pair[1],"en",{sensitivity})));
console.log("empty","a".localeCompare("A","en",{}));
const inherited=Object.create({sensitivity:"base"});console.log("inherited","a".localeCompare("A","en",inherited));
try{"a".localeCompare("b","en",{sensitivity:"invalid" as any});}catch(error){console.log("invalid",(error as Error).name);}
try{"a".localeCompare("b","en",null as any);}catch(error){console.log("null",(error as Error).name);}
const words=["b","A","a"];words.sort((a,b)=>a.localeCompare(b,"en",{sensitivity:"base"}));console.log("sort",words.join("|"));
