export {};
const FormData:any=(globalThis as any).FormData;
async function main():Promise<void>{
 const data=new FormData();
 console.log("empty",data.get("x"),data.has("x"),data.getAll("x").length,Object.prototype.toString.call(data),data instanceof FormData);
 data.append("a","1");data.append("b","2");data.append("a","3");
 console.log("ordered",Array.from(data.keys()).join(","),data.getAll("a").join(","));
 data.set("a","4");console.log("set",Array.from(data.entries()).map((pair:any)=>pair.join(":")).join(","));
 data.delete("b");data.set("c",null);data.append("d",undefined);data.append("n",123);
 console.log("coerce",Array.from(data.values()).join(","),data.has("missing"));
 data.append("bad\ud800","value\udfff");console.log("surrogates",data.get("bad\ufffd"));
 const blob=new Blob(["abc"],{type:"TEXT/PLAIN"});
 data.append("blob",blob);data.append("renamed",blob,"name.txt");
 const file=new File(["xyz"],"original.txt",{type:"text/x",lastModified:123});
 data.append("file",file);data.append("renamed file",file,"other.txt");
 for(const name of ["blob","renamed","file","renamed file"]){const value=data.get(name);console.log("file",name,value instanceof File,value.name,value.type,value.size,await value.text(),value===file);}
 console.log("modified",data.get("renamed file").lastModified,data.get("file").lastModified);
 const missing=data.getAll("missing");missing.push("x");console.log("snapshot",data.getAll("missing").length);
 const fields=new FormData();fields.append("a","1");fields.append("b","2");
 const iterator=fields.entries();console.log("iterator",Object.prototype.toString.call(iterator),iterator[Symbol.iterator]()===iterator,iterator.next().value.join(":"));
 fields.append("c","3");console.log("live",Array.from(iterator).map((pair:any)=>pair.join(":")).join(","));
 console.log("done",iterator.next().done);fields.append("d","4");console.log("resume",iterator.next().value.join(":"));
 const visited:string[]=[];fields.forEach(function(this:any,value:any,key:any,owner:any){visited.push(key+value+(owner===fields)+(this===visited));if(key==="a")owner.append("e","5");},visited);
 console.log("foreach",visited.join(","));
 class Child extends FormData {label():string{return "child";}}
 const child=new Child();child.append("x","yes");console.log("subclass",child instanceof FormData,child instanceof Child,child.get("x"),child.label());
 console.log("alias",data[Symbol.iterator]===data.entries);
 for(const name of ["append","set","get","getAll","has","delete","forEach"]){try{data[name]();}catch(error){console.log("missing",name,(error as Error).name);}}
 for(const name of ["append","set"]){try{data[name]("x","text",undefined);}catch(error){console.log("filename type",name,(error as Error).name);}}
 for(const input of [null,{},1])try{new FormData(input);}catch(error){console.log("constructor",(error as Error).name);}
 let effects="";
 const name={toString(){effects+="N";return "key";}}, value={toString(){effects+="V";return "value";}}, filename={toString(){effects+="F";return "name";}};
 data.append(name,value);console.log("convert",effects,data.get("key"));
 effects="";data.set(name,blob,filename);console.log("convert blob",effects,data.get("key").name);
 effects="";try{data.append(name,value,filename);}catch(error){console.log("convert invalid",effects,(error as Error).name);}
 const append=data.append;try{append.call({},"x","v");}catch(error){console.log("receiver",(error as Error).name);}
}
main().catch(error=>console.log("failure",String(error)));
