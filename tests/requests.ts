export {};
async function main():Promise<void>{
 const request=new Request("https://example.com/path?q=1#fragment");
 console.log("defaults",request.url,request.method,request.destination,request.referrer,request.referrerPolicy,request.mode,request.credentials,request.cache,request.redirect,request.integrity,request.keepalive,request.body===null,request.bodyUsed,(request as any).duplex,(request as any).isReloadNavigation,(request as any).isHistoryNavigation);
 const post=new Request(new URL("https://example.com/post"),{method:"post",body:"hello",headers:{"x-test":"one"},credentials:"include",cache:"no-cache",redirect:"manual",referrer:"https://referrer.example/path",referrerPolicy:"origin",integrity:"sha256-value",keepalive:true});
 console.log("post",post.url,post.method,post.headers.get("content-type"),post.headers.get("x-test"),post.credentials,post.cache,post.redirect,post.referrer,post.referrerPolicy,post.integrity,post.keepalive);
 const clone=post.clone();clone.headers.set("x-test","copy");console.log("clone headers",post.headers.get("x-test"),clone.headers.get("x-test"),post.signal!==clone.signal);
 console.log("bodies",await post.text(),await clone.text(),post.bodyUsed,clone.bodyUsed);
 const original=new Request("https://example.com/",{method:"POST",body:"source"});const moved=new Request(original);
 console.log("transfer",original.bodyUsed,original.body!.locked,moved.bodyUsed,moved.body!==original.body,await moved.text());
 try{await original.text();}catch(error){console.log("moved source",(error as Error).name);}
 const base=new Request("https://example.com/",{method:"POST",body:"old"});const override=new Request(base,{body:"new",headers:{"x-test":"override"}});console.log("override",base.bodyUsed,override.headers.get("content-type"),await override.text(),await base.text());
 const nullable=new Request("https://example.com/",{method:"POST",body:"kept"});console.log("null override",await new Request(nullable,{body:null}).text(),nullable.bodyUsed);
 console.log("json",JSON.stringify(await new Request("https://example.com/",{method:"POST",body:JSON.stringify({x:7})}).json()));
 const params=new Request("https://example.com/",{method:"POST",body:new URLSearchParams({q:"hello world"})});console.log("params",params.headers.get("content-type"),await params.text());
 const input=new Uint8Array([1,2,3]);const binary=new Request("https://example.com/",{method:"PUT",body:input});input[0]=9;console.log("snapshot",[...new Uint8Array(await binary.arrayBuffer())].join(","));
 for(const method of ["get","head","delete","options","put","post","PATCH","custom"]){console.log("method",method,new Request("https://example.com/",{method}).method);}
 const configured=new Request("https://example.com/",{referrer:"https://referrer.example/path",referrerPolicy:"origin",credentials:"include",cache:"no-store",redirect:"manual",integrity:"hash",mode:"same-origin"});
 const inherited=new Request(configured);const modified=new Request(configured,{headers:{x:"value"}});
 console.log("inherited metadata",inherited.referrer,inherited.referrerPolicy,inherited.credentials,inherited.mode,inherited.cache,inherited.redirect,inherited.integrity);
 console.log("modified metadata",modified.referrer,modified.referrerPolicy,modified.credentials,modified.mode,modified.cache,modified.redirect,modified.integrity);
 const noCors=new Request("https://example.com/",{mode:"no-cors",method:"POST",headers:{"x-test":"one","content-type":"application/json"}});console.log("no cors",noCors.mode,noCors.headers.get("x-test"),noCors.headers.get("content-type"));
 class Custom extends Request{label="custom";}
 const custom=new Custom("https://example.com/");console.log("subclass",custom.label,custom instanceof Request,custom instanceof Custom,custom.clone() instanceof Custom);
 console.log("global",globalThis.Request===Request,Object.prototype.toString.call(request),Request.length,await request.text(),request.bodyUsed);
}
main().catch(error=>{console.log("unexpected",String(error));throw error;});
