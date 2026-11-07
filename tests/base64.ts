export {};
for (const text of ["","a","ab","abc","hello",String.fromCharCode(0,128,255),"undefined"]) console.log("roundtrip",JSON.stringify(text),btoa(text),atob(btoa(text))===text);
for (const text of ["YQ","YWI","YQ=="," Y\tQ=\n=\r\f","", "AA", "AB", "//8="]) console.log("decode",JSON.stringify(text),Array.from(atob(text),c=>c.charCodeAt(0)).join(","));
for (const text of ["a","YQ=","YQ===","=YQ=","YQ==A","YQ-_","YQ\v==","YQ\u00a0=="]) try {atob(text);console.log("unexpected",JSON.stringify(text));} catch(error) {console.log("invalid",JSON.stringify(text),(error as Error).name);}
for (const text of ["\u0100","\ud800","\ud83d\ude00"]) try {btoa(text);console.log("unexpected encode");} catch(error) {console.log("wide",(error as Error).name);}
const encode=btoa,decode=atob;
console.log("alias",decode(encode("hello")),btoa.name,btoa.length,atob.name,atob.length);
console.log("coerce",btoa(123 as any),btoa({toString:()=>"ok"} as any));
function shadow(btoa:(x:string)=>string):string{return btoa("abc");}console.log("shadow",shadow(x=>"local:"+x));

for (const fn of [atob,btoa]) try {(fn as any)();} catch(error) {console.log("missing",(error as Error).name);}

let binary="";for(let i=0;i<256;i++)binary+=String.fromCharCode(i);
console.log("all bytes",btoa(binary),atob(btoa(binary))===binary);
console.log("receiver",btoa.call(null,"abc"),atob.call({},"YWJj"));
