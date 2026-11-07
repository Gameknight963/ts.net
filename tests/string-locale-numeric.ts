for(const numeric of [true,false])for(const pair of [["file2","file10"],["10","2"],["2","02"],["a100b2","a100b10"],["-2","-10"],["1.2","1.10"],["\u0662","\u0661\u0660"]])console.log(numeric,pair.join("|"),Math.sign(pair[0].localeCompare(pair[1],"en",{numeric})));
const values=["file10","file1","file2"];values.sort((a,b)=>a.localeCompare(b,"en",{numeric:true}));console.log(values.join("|"));
console.log("combined","A2".localeCompare("a02","en",{numeric:true,sensitivity:"base"}));
const options=Object.create({numeric:true});console.log("inherited",Math.sign("2".localeCompare("10","en",options)));
