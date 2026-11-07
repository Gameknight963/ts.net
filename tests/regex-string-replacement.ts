console.log("abc abc".replace(/(a)(b)/g,"$2$1-$&-$$-$`-$'-$01-$10-$99-$0"));
console.log("ab a".replace(/a(b)?/g,"[$1]"));
console.log("abc".replace(/(?:)/g,"-"));
console.log("abc".replace(/b/,"x"),"aba".replaceAll(/a/g,"x"));
const global=/a/g; global.lastIndex=2;
console.log("aba".replace(global,(match,index,text)=>{console.log("callback",match,index,text,global.lastIndex);return "X";}),global.lastIndex);
const sticky=/a/y; sticky.lastIndex=1;console.log("baac".replace(sticky,"X"),sticky.lastIndex);
sticky.lastIndex=0;console.log("baac".replace(sticky,"X"),sticky.lastIndex);
const both=/a/gy;console.log("aaba".replace(both,"X"),both.lastIndex);
const plain=/a/;plain.lastIndex=100;console.log("aba".replace(plain,"X"),plain.lastIndex);
console.log("ab a".replace(/a(b)?/g,(match,capture,index,text)=>{console.log("capture",match,capture,index,text);return "X";}));
try{console.log("abc".replaceAll(/a/,"x"));}catch(error){console.log((error as Error).name);}
console.log("abc".replace(/z/g,{toString(){console.log("convert");return "x";}} as any));
