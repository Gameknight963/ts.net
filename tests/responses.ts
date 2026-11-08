export {};
async function main():Promise<void>{
 const response=new Response("hello",{status:201,statusText:"Created",headers:{"x-test":"one"}});
 console.log("metadata",response.status,response.statusText,response.ok,response.type,response.url,response.redirected,response.headers.get("content-type"),response.headers.get("x-test"),response.bodyUsed,response.body!==null);
 const clone=response.clone();clone.headers.set("x-test","copy");console.log("headers",response.headers.get("x-test"),clone.headers.get("x-test"));
 console.log("text",await response.text(),response.bodyUsed,response.body!.locked,await clone.text());
 try{await response.text();}catch(error){console.log("repeat",(error as Error).name);}
 try{response.clone();}catch(error){console.log("clone used",(error as Error).name);}
 const nil=new Response();console.log("null body",nil.body===null,await nil.text(),nil.bodyUsed,await nil.text());
 const empty=new Response("");console.log("empty body",empty.body!==null,await empty.text(),empty.bodyUsed);
 const json=Response.json({value:7,list:[1,2]},{status:202});console.log("json metadata",json.status,json.headers.get("content-type"));console.log("json",JSON.stringify(await json.json()));
 const raw=new Uint8Array([65,66,67]);const binary=new Response(raw);raw[0]=90;const bytes=new Uint8Array(await binary.arrayBuffer());console.log("snapshot",[...bytes].join(","),binary.bodyUsed);
 const source=new Uint8Array([0,72,73,0]);console.log("view",await new Response(new DataView(source.buffer,1,2)).text());
 const blob=await new Response(new Blob(["blob"],{type:"TEXT/PLAIN"})).blob();console.log("blob",blob.size,blob.type,await blob.text());
 const params=new Response(new URLSearchParams({a:"hello world"}));console.log("params",params.headers.get("content-type"),await params.text());
 console.log("bytes",[...await new Response("bytes").bytes()].join(","));
 const bom=new Response(new Uint8Array([239,187,191,123,125]));console.log("bom",JSON.stringify(await bom.json()));
 const error=Response.error();console.log("error",error.status,error.statusText,error.ok,error.type,error.body===null);try{error.headers.set("x","value");}catch(e){console.log("immutable error",(e as Error).name);}
 const redirect=Response.redirect("https://example.com/path",307);console.log("redirect",redirect.status,redirect.headers.get("location"),redirect.type);try{redirect.headers.delete("location");}catch(e){console.log("immutable redirect",(e as Error).name);}
 class Custom extends Response{label="custom";}
 const custom=new Custom("value");console.log("subclass",custom.label,custom instanceof Response,custom instanceof Custom,custom.clone() instanceof Custom,await custom.text());
 console.log("global",globalThis.Response===Response,Object.prototype.toString.call(new Response()),Response.length);
}
main().catch(error=>{console.log("unexpected",String(error));throw error;});
