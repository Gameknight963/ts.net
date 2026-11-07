for(const text of ["a\nb", "a\rb", "a\u2028b", "a\u2029b", "a b"]){console.log(/a.b/s.test(text),new RegExp("a.b","s").test(text));}
const regex=/(a).(b)/gims;console.log(regex.flags,regex.dotAll,regex.source,String(regex));console.log(JSON.stringify(regex.exec("a\nb")));
console.log("replace","a\nb a\rb".replace(/a.b/gs,"X"));console.log("split",JSON.stringify("xa\nby".split(/a.b/s)));
console.log("match",JSON.stringify("a\nb a\rb".match(/a.b/gs)));console.log("all",JSON.stringify([..."a\nb".matchAll(/a.b/gs)]));
console.log("classes",/a[.]b/s.test("a\nb"),/a\.b/s.test("a\nb"));
const clone=new RegExp(regex);console.log(clone.dotAll,clone.flags,clone.source);
