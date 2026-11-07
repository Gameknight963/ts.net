for(const text of ["", "abc XYZ", "Hello, World!", "123!", "\u00e9\u00c9", "\u0410\u0431\u0412", "\u03b1\u03b2\u03b3", "\u4e2d\ud83d\ude00", "\ud800", "\udc00"]){console.log(JSON.stringify(text.toLowerCase()),JSON.stringify(text.toUpperCase()));}
const upper:any=String.prototype.toUpperCase,lower:any=String.prototype.toLowerCase;
for(const value of [123,true,false,new String("Hello"),{toString(){return "Hello";}}])console.log(upper.call(value),lower.call(value));
for(const method of [upper,lower])for(const value of [null,undefined])try{method.call(value);}catch(error){console.log("invalid",(error as Error).name);}
console.log("metadata",upper.name,upper.length,lower.name,lower.length);
console.log("chain","AbCd".toLowerCase().toUpperCase());
