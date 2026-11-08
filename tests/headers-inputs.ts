export {};
const fromMap=new Headers(new Map([["B","two"],["A","one"]]) as any);console.log("map",[...fromMap].map(pair=>pair.join("=")).join("|"));
const record=Object.create({inherited:"ignored"},{x:{value:"one",enumerable:true},hidden:{value:"ignored"}});
console.log("record",new Headers(record).get("x"),new Headers(record).has("inherited"),new Headers(record).has("hidden"));
const cookie=new Headers();cookie.append("Cookie","a=1");cookie.append("Cookie","b=2");console.log("cookie",cookie.get("cookie"));
let effects="";
function text(value:string):any{return {toString(){effects+=value;return value;}};}
const pair:any[]=[[text("x"),text("a")],[text("X"),text("b")]];
console.log("conversion",new Headers(pair as any).get("x"),effects);
function* entries():Generator<string[]>{try{effects+="1";yield ["a","one"];effects+="2";yield ["b","two"];}finally{effects+="F";}}
effects="";console.log("generator",[...new Headers(entries() as any).keys()].join(","),effects);
function* invalid():Generator<any>{try{yield ["a","one"];yield ["b","\u0100"];effects+="unreached";}finally{effects+="closed";}}
effects="";try{new Headers(invalid() as any);}catch(error){console.log("close",(error as Error).name,effects);}
for(const name of ["append","set","get","has","delete","forEach"]){try{(Headers.prototype as any)[name].call(new Headers());console.log("missing accepted");}catch(error){console.log("missing",name,(error as Error).name);}}
for(const name of ["append","set","get","has","delete","forEach","keys","values","entries","getSetCookie"]){try{(Headers.prototype as any)[name].call({},"x","y");console.log("receiver accepted");}catch(error){console.log("receiver",name,(error as Error).name);}}
function read(value:Pick<Headers,"get">):string|null{return value.get("a");}
console.log("view",read(fromMap));
