export {};
async function main():Promise<void>{
 const url="https://example.com/";
 const options:any[]=[{method:"CONNECT"},{method:"trace"},{method:"TRACK"},{method:"bad name"},{method:""},{method:"GET",body:""},{method:"HEAD",body:"x"},{mode:"navigate"},{mode:"wrong"},{mode:"no-cors",method:"PUT"},{cache:"only-if-cached"},{credentials:"wrong"},{redirect:"wrong"},{referrerPolicy:"wrong"},{signal:{}},{window:{}},{duplex:"full"},{headers:null},{referrer:"relative"}];
 for(const init of options)try{new Request(url,init);console.log("invalid accepted");}catch(error){console.log("invalid",(error as Error).name);}
 for(const value of ["", "relative", "https://user:pass@example.com/"])try{new Request(value);console.log("url accepted");}catch(error){console.log("url",(error as Error).name);}
 const cached=new Request(url,{mode:"same-origin",cache:"only-if-cached"});console.log("cached",cached.mode,cached.cache);
 const source=new Request(url,{method:"POST",body:"value"});await source.text();
 try{source.clone();}catch(error){console.log("clone used",(error as Error).name);}
 try{new Request(source);}catch(error){console.log("copy used",(error as Error).name);}
 console.log("replace used",await new Request(source,{body:"replacement"}).text());
 const locked=new Request(url,{method:"POST",body:"locked"});const reader=locked.body!.getReader();try{locked.clone();}catch(error){console.log("clone locked",(error as Error).name);}reader.releaseLock();console.log("released",await locked.text());
 const stream=new Response("stream");try{new Request(url,{method:"POST",body:stream.body});}catch(error){console.log("duplex required",(error as Error).name);}
 const streamed=new Request(url,{method:"POST",body:stream.body,duplex:"half"} as any);console.log("stream body",streamed.body===stream.body,await streamed.text(),stream.bodyUsed);
 for(const init of [{method:"POST",body:new Response("x").body,duplex:"half",keepalive:true},{method:"POST",body:new Response("x").body,duplex:"half",mode:"no-cors"}])try{new Request(url,init as any);console.log("stream accepted");}catch(error){console.log("stream option",(error as Error).name);}
 const wrong=Request.prototype.text.call({} as Request);console.log("wrong promise",wrong instanceof Promise);try{await wrong;}catch(error){console.log("receiver",(error as Error).name);}
}
main().catch(error=>{console.log("unexpected",String(error));throw error;});
