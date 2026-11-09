export {};
const origin='http://127.0.0.1:0';
async function run():Promise<void>{
 const first=await fetch(origin+'/text');
 console.log('get',first.status,first.ok,first.type,first.url===origin+'/text',first.headers.get('x-test'),first.bodyUsed);
 const clone=first.clone();console.log('text',await first.text(),await clone.text(),first.bodyUsed);
 try{first.headers.set('x-test','changed');}catch(error){console.log('immutable',(error as Error).name);}
 const posted=await fetch(new Request(origin+'/echo',{method:'POST',headers:{'x-test':'custom'},body:'payload'}));
 const echo=await posted.json() as {method:string;body:string;header:string;contentType:string};
 console.log('post',echo.method,echo.body,echo.header,echo.contentType);
 for(const endpoint of ['redirect303','redirect307']){
  const response=await fetch(origin+'/'+endpoint,{method:'POST',body:'redirect body'});const value=await response.json() as {method:string;body:string;contentType:string|null};
  console.log(endpoint,response.redirected,value.method,value.body,value.contentType);
 }
 const followed=await fetch(origin+'/redirect');console.log('follow',followed.redirected,followed.url===origin+'/text',await followed.text());
 const manual=await fetch(origin+'/redirect',{redirect:'manual'});console.log('manual',manual.status,manual.redirected,manual.headers.get('location'));
 for(const endpoint of ['redirect','loop']){try{await fetch(origin+'/'+endpoint,endpoint==='redirect'?{redirect:'error'}:{});}catch(error){console.log('redirect error',(error as Error).name);}}
 const missing=await fetch(origin+'/missing');console.log('status',missing.status,missing.ok,await missing.text());
 for(const method of ['GET','HEAD']){const empty=await fetch(origin+'/empty',{method});console.log('empty',method,empty.status,empty.body===null,await empty.text());}
 for(const coding of ['gzip','deflate','brotli']){const response=await fetch(origin+'/'+coding);console.log('encoding',coding,response.headers.get('content-encoding'),await response.text());}
 const controller=new AbortController();const reason=new Error('stop');
 const pending=fetch(origin+'/slow',{signal:controller.signal});setTimeout(()=>controller.abort(reason),5);
 try{await pending;}catch(error){console.log('abort',error===reason);}
 const already=new AbortController();already.abort(reason);
 try{await fetch(origin+'/text',{signal:already.signal});}catch(error){console.log('already aborted',error===reason);}
 try{await fetch(origin+'/slow',{signal:AbortSignal.timeout(5)});}catch(error){console.log('timeout',(error as Error).name);}
 const concurrent=await Promise.all([fetch(origin+'/text'),fetch(origin+'/text')]);console.log('concurrent',(await Promise.all(concurrent.map(value=>value.text()))).join(','));
 console.log('metadata',fetch.name,fetch.length,globalThis.fetch===fetch);
 const streamed=await fetch(origin+'/text');const byteReader=streamed.body!.getReader({mode:'byob'});
 const prefix=await byteReader.read(new Uint8Array(2));byteReader.releaseLock();
 const rest=streamed.body!.getReader();
 console.log('byob',Array.from(prefix.value!).join(','),Array.from((await rest.read()).value!).join(','),(await rest.read()).done,streamed.bodyUsed);rest.releaseLock();
}
run();
