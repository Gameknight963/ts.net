function show(text:string,pattern:any):void {
 const result=text.match(pattern);console.log(JSON.stringify(result),result?.index,result?.input,result?.groups);
 console.log("search",text.search(pattern));
}
for(const pattern of [/a(b)?/,/a(b)?/g,/a/y,/a/gy,/(?:)/g,/$/g,/z/,"a(b)?",undefined])for(const text of ["", "ab a", "ba", "aaa"])show(text,pattern);
for(const flags of ["", "g", "y", "gy"]){
 const regex=new RegExp("a",flags);regex.lastIndex=1;show("baaa",regex);console.log("last",flags,regex.lastIndex);
 regex.lastIndex=100;console.log("search index","ba".search(regex),regex.lastIndex);
}
const preserved=/a/g;const marker={valueOf(){console.log("unexpected conversion");return 1;}};(preserved as any).lastIndex=marker;
console.log("preserved","ba".search(preserved),(preserved as any).lastIndex===marker);
console.log("generic",(String.prototype.match as any).call(123,/2/)[0],(String.prototype.search as any).call(123,/2/));
console.log("units","??".match(/(?:)/g)?.length);
console.log("metadata",String.prototype.match.name,String.prototype.match.length,String.prototype.search.name,String.prototype.search.length);
