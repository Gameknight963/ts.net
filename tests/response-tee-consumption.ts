async function run():Promise<void>{
 const [a,b]=new Response(new Uint8Array([1,2,3])).body!.tee();
 const exposed=new Uint8Array(await new Response(a).arrayBuffer());exposed[0]=99;
 console.log('array buffer',Array.from(new Uint8Array(await new Response(b).arrayBuffer())).join(','));
 const [c,d]=new Response('cloned').body!.tee();
 const cancellation=c.cancel();
 const response=new Response(d);const clone=response.clone();
 console.log('cloned',await clone.text(),response.bodyUsed);
 await cancellation;console.log('cancel after clone');
 console.log('original',await response.text());
 const [e,f]=new Response('request clone').body!.tee();
 const canceled=e.cancel();
 const request=new Request('https://example.test/',{method:'POST',body:f,duplex:'half'} as RequestInit);
 const requestClone=request.clone();
 console.log('request clone',await requestClone.text(),request.bodyUsed);
 await canceled;console.log('request canceled');
 console.log('request original',await request.text());
}
run();
