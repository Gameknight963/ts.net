function tag(parts:TemplateStringsArray,...values:any[]):string{console.log("tag",JSON.stringify(parts),JSON.stringify(parts.raw));return parts.join("|")+":"+values.join("|");}
async function run():Promise<void>{
 console.log("begin");console.log(tag`a${await Promise.resolve(1)}b${await 2}c`);
 console.log((await Promise.resolve(tag))`d${await 3}e`);
 const object={prefix:"object",tag(parts:TemplateStringsArray,value:number):string{return this.prefix+":"+parts[0]+value;}};
 console.log(object.tag`x${await 4}`);console.log(String.raw`raw${await 5}end`);
 try{tag`error${await Promise.reject("rejected")}`;}catch(error){console.log("catch",error);}
 console.log("done");
}
run();console.log("outside");
