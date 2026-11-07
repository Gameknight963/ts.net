export {};
async function main():Promise<void>{
 const file=new File(["hello",new Uint8Array([32,65])],"notes.txt",{type:"TEXT/PLAIN",lastModified:123456789});
 console.log("metadata",file.name,file.size,file.type,file.lastModified,Object.prototype.toString.call(file));
 console.log("inheritance",file instanceof File,file instanceof Blob,Object.getPrototypeOf(File)===Blob,Object.getPrototypeOf(File.prototype)===Blob.prototype);
 console.log("read",await file.text(),Array.from(await file.bytes()).join(","));
 const slice=file.slice(0,5);console.log("slice",slice instanceof Blob,slice instanceof File,slice.type,await slice.text());
 const nested=new Blob([file]);console.log("nested",await nested.text());
 class Child extends File{label():string{return this.name;}}
 const child=new Child(["child"],"child.txt",{lastModified:0});console.log("subclass",child instanceof Child,child instanceof File,child instanceof Blob,child.label(),await child.text());
 for(const lastModified of [0,-1,1.9,NaN,Infinity,18446744073709551616,9223372036854775808])console.log("timestamp",String(lastModified),new File([],"x",{lastModified}).lastModified);
 console.log("name",new File([],"a/b\\c").name,new File([],"a\ud800b").name);
 const named=new File([], {toString:()=>"custom.txt"} as any, Object.create({type:"TEXT/X",lastModified:42}));console.log("coerce",named.name,named.type,named.lastModified);
 const before=Date.now();const current=new File([],"now");console.log("default time",current.lastModified>=before,current.lastModified<=Date.now());
 for(const args of [[],[[]],[null,"x"],[undefined,"x"],["abc","x"]])try{Reflect.construct(File,args);console.log("unexpected");}catch(error){console.log("invalid",(error as Error).name);}
}
main().catch(error=>console.log("failure",String(error)));
