const escapeRegex:any=(RegExp as any).escape;
for(const text of ["", "foo", "123", "a.b+c?", "foo-bar", "(hello)", " a\n\t\r\v\f", "^$\\.*+?()[]{}|/", ",-=<>#&!%:;@~'`\"", "\u00e9\u4e2d\ud83d\ude00", "\u00a0\u1680\u2000\u2028\u2029\u202f\u205f\u3000\ufeff", "\ud800", "\udc00", "\ud800X\udc00"]){
 const escaped=escapeRegex(text);console.log(JSON.stringify(escaped),new RegExp("^(?:"+escaped+")$").test(text));
}
let hash=0,total=0;
for(let code=0;code<65536;code++){
 const value=escapeRegex(String.fromCharCode(code));total+=value.length;
 for(let index=0;index<value.length;index++)hash=((hash*33)^value.charCodeAt(index))>>>0;
}
console.log("all code units",hash,total);
for(const value of [undefined,null,12,true,{},new String("abc")])try{escapeRegex(value);}catch(error){console.log("invalid",(error as Error).name);}
console.log("metadata",escapeRegex.name,escapeRegex.length,Object.keys(RegExp).includes("escape"));
console.log("borrowed",escapeRegex.call(null,"abc"));
