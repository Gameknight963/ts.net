export {};
async function main():Promise<void>{
 const data:FormData=new FormData();data.append("a","1");data.append("a","2");
 const field:FormDataEntryValue|null=data.get("a");console.log("typed",field,data.getAll("a").join(","));
 data.set("b",new Blob(["text"]),"file.txt");
 const file=data.get("b");if(file instanceof File)console.log("typed file",file.name,await file.text());
 const iterator=data.entries();console.log("iterator",iterator.next().value![0],Array.from(data.keys()).join(","));
 data.forEach((value,key,owner)=>console.log("field",key,typeof value,owner===data));
 function use(value:FormData):FormData {value.append("c","3");return value;}
 console.log("parameter",use(data)===data,data.get("c"));
 class Child extends FormData {label():string{return "child";}}
 const child=new Child();child.append("x","yes");console.log("child",child instanceof FormData,child instanceof Child,child.get("x"),child.label());
}
main().catch(error=>console.log("failure",String(error)));
