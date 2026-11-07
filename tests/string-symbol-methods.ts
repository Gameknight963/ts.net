export {};
for(const [name,symbol] of [["match",Symbol.match],["search",Symbol.search],["replace",Symbol.replace],["replaceAll",Symbol.replace],["split",Symbol.split],["matchAll",Symbol.matchAll]] as const){
 const method=(String.prototype as any)[name];
 const receiver={toString(){console.log("unexpected conversion");return "text";}};
 const pattern:any={};pattern[symbol]=function(this:any,...args:any[]):string {console.log("hook",name,this===pattern,args.length,args[0]===receiver,args[1]);return "custom";};
 console.log("result",name,method.call(receiver,pattern,7));
 const inherited=Object.create(pattern);console.log("inherited",name,method.call("abc",inherited,"x"));
 pattern[symbol]=1;try{method.call("abc",pattern);}catch(error){console.log("not callable",name,(error as Error).name);}
 pattern[symbol]=null;pattern.toString=()=>"b";console.log("fallback",name,Array.isArray(method.call("abc",pattern,"x")));
 try{method.call(null,pattern);}catch(error){console.log("null receiver",name,(error as Error).name);}
}
const regex=/a/g;regex[Symbol.replace]=function(text:string,replacement:any):string{return "override:"+text+":"+replacement;};
console.log("regex override","abc".replace(regex,"x"),"abc".replaceAll(regex,"x"));
const splitter={[Symbol.split](text:string,limit?:number){return [text,String(limit)];}};console.log("ordinary split","abc".split(splitter,2).join("|"));

const stringPrototype:any=String.prototype;
stringPrototype[Symbol.split]=function(this:string,text:string,limit:number){return ["primitive",String(this),text,String(limit)];};
console.log("primitive hook","abc".split("x",3).join("|"));delete stringPrototype[Symbol.split];
