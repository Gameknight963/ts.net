for(const pattern of [/(?<=a)b/d,/(?<!a)b/d,/(?<=a|bc)d/d,/(?<=([ab]+)([bc]+))$/d,/(?<=(a+))b/d,/(?<!(a))b/d])for(const text of ["", "ab", "bb", "bcd", "abc", "aaab"]){const value=pattern.exec(text);console.log(JSON.stringify(value),value?.index,JSON.stringify(value?.indices));}
console.log("replace","ab cb ab".replace(/(?<=a)b/g,"X"));console.log("split",JSON.stringify("abcabc".split(/(?<=b)/)));
console.log("all",JSON.stringify([..."ab ab".matchAll(/(?<=a)b/g)]));console.log("dot",/(?<=a.)b/s.test("a\nb"));
const constructed=new RegExp("(?<=a)b","g");console.log(constructed.source,constructed.test("ab"),constructed.lastIndex);
