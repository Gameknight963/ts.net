function show(text:string, separator:RegExp, limit?:number):void {console.log(JSON.stringify(text.split(separator,limit)));}
for(const pattern of [/,/, /,/g, /,/y, /(,)/, /(,)|(x)/, /(?:)/, /(?=b)/, /$/,/^/, /a*/, /b*/]) {
 for(const text of ["", "abc", "a,b,,c,", "aaa", "b", "ab"]) show(text,pattern);
}
for(const limit of [0,1,2,3,4,-1,NaN,Infinity,1.9,4294967296,4294967297])show("a,b,c",/(,)/,limit);
const regex=/,/gy;regex.lastIndex=20;show("a,b,c",regex);console.log("index",regex.lastIndex);
const units="??x".split(/(?:)/);console.log(units.length,units[0].charCodeAt(0),units[1].charCodeAt(0));
show("AbCA",/a/i);show("ab",/(a)(b)/,2);
console.log((String.prototype.split as any).call(123,/2/).join("|"));
