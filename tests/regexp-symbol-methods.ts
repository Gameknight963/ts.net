export {};
const symbols=[Symbol.match,Symbol.matchAll,Symbol.replace,Symbol.search,Symbol.split];
for(const symbol of symbols){const method=(RegExp.prototype as any)[symbol];console.log("method",method.name,method.length,typeof method);try{method.call(null,"abc");}catch(error){console.log("brand",(error as Error).name);}}
console.log("match",JSON.stringify(/a/g[Symbol.match]("aba")),JSON.stringify(/(?<x>a)(b)/[Symbol.match]("abc")));
const search=/a/g;search.lastIndex=9;console.log("search",search[Symbol.search]("ba"),search.lastIndex);
console.log("replace",/a/g[Symbol.replace]("aba","x"),/(?<x>a)/[Symbol.replace]("ab","$<x>!"));
console.log("split",JSON.stringify(/(,)/[Symbol.split]("a,b,c",4)));
for(const pattern of [/a/,/a/g,/(?:)/g]){const iterator=pattern[Symbol.matchAll]("aba");let count=0;for(const match of iterator){console.log("all",pattern.flags,match.index,match[0]);if(++count>10)break;}console.log("done",iterator.next().done);}
const overridden=/a/g;overridden[Symbol.match]=()=>["custom"];
console.log("override","aba".match(overridden)!.join("|"),RegExp.prototype[Symbol.match].call(overridden,"aba")!.join("|"));
console.log("coerce",/null/[Symbol.match](null as any)![0]);
