export {};
const headers=new Headers([["X-Test"," first "],["x-test","second"],["Z","last"],["a","start"]]);
console.log("get",headers.get("X-TEST"),headers.has("x-test"),headers.get("missing"));
console.log("entries",Array.from(headers,([name,value])=>name+"="+value).join("|"));
console.log("keys",[...headers.keys()].join(","),[...headers.values()].join("|"));
headers.set("x-test"," replaced ");headers.delete("z");headers.append("empty","");
console.log("mutate",[...headers].map(([n,v])=>n+"="+v).join("|"));
headers.append("Set-Cookie","a=1");headers.append("set-cookie","b=2");
console.log("cookies",headers.get("set-cookie"),headers.getSetCookie().join("|"),[...headers].filter(([name])=>name==="set-cookie").map(([,value])=>value).join("|"));
const copy=new Headers(headers);copy.append("set-cookie","c=3");
console.log("copy",headers.getSetCookie().length,copy.getSetCookie().length);
const fromIterator=new Headers(headers.entries() as any);console.log("iterator init",fromIterator.get("x-test"));
const record=new Headers({"B":"two","a":"one"});console.log("record",[...record.keys()].join(","));
const cookies=headers.getSetCookie();cookies.push("local");console.log("cookie copy",headers.getSetCookie().length);
let effects="";
headers.forEach(function(this:{tag:string},value,key,owner){effects+=this.tag+key+":"+value+":"+(owner===headers)+";";},{tag:"!"});
console.log("foreach",effects);
const live=new Headers({b:"2",d:"4"});const iterator=live.entries();console.log("live first",iterator.next().value!.join("="));
live.append("c","3");console.log("live rest",Array.from(iterator,entry=>entry.join("=")).join("|"));
live.append("z","9");console.log("done",iterator.next().done);
const mutate=new Headers({a:"1",b:"2"});const seen:string[]=[];
mutate.forEach((value,key)=>{seen.push(key+value);if(key==="a")mutate.append("c","3");});console.log("live foreach",seen.join(","));
class CustomHeaders extends Headers{label="custom";read(){return this.get("x");}}
const custom=new CustomHeaders({x:"value"});console.log("subclass",custom.read(),custom.label,custom instanceof Headers,custom instanceof CustomHeaders);
console.log("global",globalThis.Headers===Headers,Object.prototype.toString.call(headers),Object.prototype.toString.call(headers.entries()),Headers.length);
for(const name of ["append","set","get","has","delete","getSetCookie","forEach","entries","keys","values"]){const method=(Headers.prototype as any)[name];const descriptor=Object.getOwnPropertyDescriptor(Headers.prototype,name)!;console.log("metadata",name,method.name,method.length,descriptor.enumerable);}
const invalid:unknown[]=[null,1,"x",[["x"]],[["x","a","b"]],[["bad name","x"]],[["x","a\rb"]],[["x","\u0100"]]];
for(const value of invalid)try{new Headers(value as HeadersInit);console.log("invalid accepted");}catch(error){console.log("invalid",(error as Error).name);}
for(const value of ["", "bad name", "a:b", "?"])try{headers.get(value);console.log("name accepted");}catch(error){console.log("name",(error as Error).name);}
for(const value of ["\r\ntrim\r\n", "tab\tvalue", "\0", "a\nb", "?"]){try{headers.set("value",value);console.log("value",JSON.stringify(headers.get("value")));}catch(error){console.log("value",(error as Error).name);}}
try{Headers.prototype.get.call({} as Headers,"x");}catch(error){console.log("receiver",(error as Error).name);}
