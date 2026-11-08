export {};
async function main():Promise<void>{
 for(const status of [0,199,600,NaN,Infinity,null,65536+201])try{console.log("status",status,new Response(null,{status} as any).status);}catch(error){console.log("status",status,(error as Error).name);}
 for(const status of [204,205,304]){console.log("null status",new Response(null,{status}).status);try{new Response("",{status});}catch(error){console.log("body status",status,(error as Error).name);}}
 for(const statusText of ["OK","?","bad\r\ntext","\0","\u0100",null])try{console.log("reason",JSON.stringify(statusText),new Response(null,{statusText} as any).statusText);}catch(error){console.log("reason",JSON.stringify(statusText),(error as Error).name);}
 for(const data of [undefined,1n,()=>1])try{Response.json(data);console.log("json accepted");}catch(error){console.log("invalid json",(error as Error).name);}
 for(const text of ["not json",""]){const response=new Response(text);try{await response.json();}catch(error){console.log("parse",(error as Error).name,response.bodyUsed);}}
 const noBody=new Response();try{await noBody.json();}catch(error){console.log("null parse",(error as Error).name,noBody.bodyUsed);}
 for(const status of [200,300,309])try{Response.redirect("https://example.com/",status);console.log("redirect accepted");}catch(error){console.log("redirect status",status,(error as Error).name);}
 try{Response.redirect("relative");}catch(error){console.log("relative",(error as Error).name);}
 const wrong=Response.prototype.text.call({} as Response);console.log("wrong promise",wrong instanceof Promise);try{await wrong;}catch(error){console.log("wrong receiver",(error as Error).name);}
 const custom=Response.json({a:1},{headers:{"content-type":"application/custom"}});console.log("override",custom.headers.get("content-type"),await custom.text());
 const error=Response.error().clone();try{error.headers.append("x","value");}catch(e){console.log("clone immutable",(e as Error).name);}
}
main().catch(error=>{console.log("unexpected",String(error));throw error;});
