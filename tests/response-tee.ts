async function run():Promise<void>{
 const response=new Response('hello');
 const source=response.body!;
 const [first,second]=source.tee();
 console.log('locked',source.locked,first.locked,second.locked,response.bodyUsed);
 const a=first.getReader(),b=second.getReader();
 console.log('first',new TextDecoder().decode((await a.read()).value),response.bodyUsed);
 console.log('second',new TextDecoder().decode((await b.read()).value));
 console.log('done',(await a.read()).done,(await b.read()).done,source.locked);
 a.releaseLock();b.releaseLock();
 const cancelSource=new Blob(['cancel test']).stream();
 const [left,right]=cancelSource.tee();
 let canceled=false;const cancellation=left.cancel('left').then(()=>{canceled=true;});
 await Promise.resolve();console.log('one cancel',canceled);
 console.log('remaining',await new Response(right).text());
 await cancellation;console.log('settled',canceled,cancelSource.locked);
 const [x,y]=new Blob(['both']).stream().tee();
 const firstCancel=x.cancel('x'),secondCancel=y.cancel('y');
 console.log('different promises',firstCancel!==secondCancel);
 await Promise.all([firstCancel,secondCancel]);console.log('both canceled');
 const empty=new Response('').body!;const [e,f]=empty.tee();
 console.log('empty',(await e.getReader().read()).done,(await f.getReader().read()).done);
 const locked=new Blob(['x']).stream();const reader=locked.getReader();
 try{locked.tee();}catch(error){console.log('locked error',(error as Error).name);}reader.releaseLock();
 console.log('metadata',source.tee.name,source.tee.length);
}
run();
