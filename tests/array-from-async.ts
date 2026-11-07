const fromAsync:any=(Array as any).fromAsync;
async function run():Promise<void>{
 console.log("start");console.log("values",(await fromAsync([Promise.resolve(1),2,Promise.resolve(3)])).join("|"));
 const context={factor:3};console.log("map",(await fromAsync([1,2],async function(this:typeof context,value:number,index:number){console.log("map call",value,index);return this.factor*value+index;},context)).join("|"));
 console.log("like",(await fromAsync({0:Promise.resolve("a"),2:"c",length:3})).join("|"));console.log("text",(await fromAsync("abc")).join("|"));console.log("set",(await fromAsync(new Set([1,2]))).join("|"));
 console.log("iterator",(await fromAsync([3,4].values())).join("|"));console.log("empty",(await fromAsync([])).length);
 class Values extends Array<any>{}const subclass=await fromAsync.call(Values,[1,2]);console.log("subclass",subclass instanceof Values,subclass.join("|"));
 try{await fromAsync([1],42);}catch(error){console.log("mapper",(error as Error).name);}
 try{await fromAsync(null);}catch(error){console.log("input",(error as Error).name);}
 try{await fromAsync([Promise.reject("value rejection")]);}catch(error){console.log("rejected",error);}
 try{await fromAsync([1],()=>Promise.reject("map rejection"));}catch(error){console.log("map rejected",error);}
 const array=[1,2];console.log("live",(await fromAsync(array,(value:number,index:number)=>{if(index===0)array.push(3);return value;})).join("|"));
 const prototype:any=Object.getPrototypeOf([].values());prototype.return=function(){console.log("return");return {done:true,value:Promise.resolve().then(()=>console.log("close settled"))};};
 try{await fromAsync([1].values(),()=>Promise.reject("cleanup"));}catch(error){console.log("closed",error);}delete prototype.return;
 console.log("metadata",fromAsync.name,fromAsync.length);console.log("done");
}
run();console.log("outside");
