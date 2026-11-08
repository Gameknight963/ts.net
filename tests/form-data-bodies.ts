export {};
async function normalize(response:Response):Promise<string>{
 const type=response.headers.get("content-type")!;const boundary=type.split("boundary=")[1];
 console.log("type",type.startsWith("multipart/form-data; boundary="),boundary.length>=27);
 return (await response.text()).split(boundary).join("BOUNDARY");
}
async function main():Promise<void>{
 const data=new FormData();data.append("a","one");data.append("a","two");data.append('q"\n\r\r\n',"a\nb\rc\r\nd");
 data.append("f",new Blob([new Uint8Array([0,65,255,13,10])]),'f"\n\r.txt');
 data.append("text",new File(["file\nbody"],"?.txt",{type:"TEXT/PLAIN"}));
 const response=new Response(data);const clone=response.clone();data.append("later","absent");
 const normalized=await normalize(response);console.log("body",JSON.stringify(normalized));
 console.log("clone",(await normalize(clone))===normalized,response.bodyUsed,clone.bodyUsed);
 const request=new Request("https://example.com/",{method:"POST",body:data});
 const type=request.headers.get("content-type")!;console.log("request type",type.startsWith("multipart/form-data; boundary="));
 const boundary=type.split("boundary=")[1];console.log("request body",(await request.text()).split(boundary).join("BOUNDARY").includes('name="later"'));
 console.log("empty",JSON.stringify(await normalize(new Response(new FormData()))));
 console.log("header override",new Response(data,{headers:{"content-type":"custom/type"}}).headers.get("content-type"));
}
main().catch(error=>console.log("failure",String(error)));
