export {};
async function run(): Promise<void> {
 const blob = new Blob(['hello world'], {type:'text/plain'});
 const url = URL.createObjectURL(blob), other = URL.createObjectURL(blob);
 console.log('unique',url !== other,url.startsWith('blob:'),new URL(url).protocol);
 const response = await fetch(url);
 console.log('response',response.status,response.statusText,response.type,response.url===url,response.redirected,response.headers.get('content-type'),response.headers.get('content-length'));
 console.log('clone',await response.clone().text(),await response.text());
 try {response.headers.set('x-test','value');}catch(error){console.log('immutable',(error as Error).name);}
 for(const range of ['bytes=0-4','bytes=-5','bytes=0-999','bytes=11-','bytes=5-2','bytes=-0','bytes=a-b','bytes=0-1,3-4','items=0-1','bytes= 0-4']) {
  try {const part=await fetch(url,{headers:{range}});console.log('range',range,part.status,part.headers.get('content-range'),part.headers.get('content-length'),await part.text());}
  catch(error){console.log('range',range,(error as Error).name);}
 }
 for(const method of ['HEAD','POST','PUT','DELETE']) {
  try {const result=await fetch(url,{method});console.log('method',method,result.status,await result.text());}catch(error){console.log('method',method,(error as Error).name);}
 }
 const pending=fetch(url);URL.revokeObjectURL(url);
 console.log('pending',await (await pending).text());
 try{await fetch(url);}catch(error){console.log('revoked',(error as Error).name);}
 console.log('other',await (await fetch(other)).text());
 const controller=new AbortController(), reason=new Error('stop');
 const aborted=fetch(other,{signal:controller.signal});controller.abort(reason);
 try{await aborted;}catch(error){console.log('abort',error===reason);}
 URL.revokeObjectURL(other);URL.revokeObjectURL(other);URL.revokeObjectURL('not registered');
 const fragmented=URL.createObjectURL(blob);
 for(const suffix of ['#fragment','?query']) {
  try {const response=await fetch(fragmented+suffix);console.log('suffix',suffix,response.url===fragmented,await response.text());}catch(error){console.log('suffix',suffix,(error as Error).name);}
 }
 URL.revokeObjectURL(fragmented+'#fragment');
 try {console.log('fragment revoke',await (await fetch(fragmented)).text());}catch(error){console.log('fragment revoke',(error as Error).name);}
 URL.revokeObjectURL(fragmented);
 for(const value of [undefined,null,1,{},'text']) {
  try {URL.createObjectURL(value as Blob);console.log('invalid accepted');}catch(error){console.log('invalid',(error as Error).name);}
 }
 let conversions=0; const marker=new Error('coercion');
 const revoke: any=URL.revokeObjectURL;
 try {revoke({toString(){conversions++;throw marker;}});}catch(error){console.log('coercion',error===marker,conversions);}
 const empty=URL.createObjectURL(new Blob());const result=await fetch(empty);
 console.log('empty',result.status,result.headers.get('content-type'),result.headers.get('content-length'),await result.text());
 URL.revokeObjectURL(empty);
 console.log('metadata',URL.createObjectURL.name,URL.createObjectURL.length,URL.revokeObjectURL.name,URL.revokeObjectURL.length);
}
run();
