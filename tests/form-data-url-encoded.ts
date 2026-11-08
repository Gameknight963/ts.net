export {};
async function main():Promise<void>{
 for(const input of ["a=1&a=2&b=hello+world", "a=%C3%A9&b=%ZZ&=empty&key", "?a=1", "??a=1", "\ufeffa=1", "%EF%BB%BFa=1", "", "&&&"]){
  const response=new Response(input,{headers:{"content-type":"Application/X-WWW-Form-Urlencoded; charset=iso-8859-1"}});
  const fields:FormData=await response.formData();console.log("parsed",JSON.stringify(Array.from(fields)),fields instanceof FormData,response.bodyUsed);
  try{await response.formData();}catch(error){console.log("twice",(error as Error).name);}
 }
 const request=new Request("https://example.com/",{method:"POST",body:new URLSearchParams([["x","1"],["x","2"]])});
 console.log("request",(await request.formData()).getAll("x").join(","),request.bodyUsed);
 const original=new Response("a=1",{headers:{"content-type":"application/x-www-form-urlencoded"}});const clone=original.clone();
 console.log("clone",(await original.formData()).get("a"),(await clone.formData()).get("a"));
 console.log("null body",Array.from(await new Response(null,{headers:{"content-type":"application/x-www-form-urlencoded"}}).formData()).length);
 for(const contentType of ["text/plain","application/json","bad"]){const response=new Response("a=1",{headers:{"content-type":contentType}});try{await response.formData();}catch(error){console.log("mime",contentType,(error as Error).name,response.bodyUsed);}}
 const locked=new Response("a=1",{headers:{"content-type":"application/x-www-form-urlencoded"}});const reader=locked.body!.getReader();
 try{await locked.formData();}catch(error){console.log("locked",(error as Error).name,locked.bodyUsed);}reader.releaseLock();console.log("unlocked",(await locked.formData()).get("a"));
}
main().catch(error=>console.log("failure",String(error)));
