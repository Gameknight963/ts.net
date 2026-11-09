export {};
async function failure(label:string,action:()=>any):Promise<void>{try{await action();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
async function main():Promise<void>{
    const response=new Response('locked');const reader=response.body!.getReader({mode:'byob'});
    console.log('unused lock',response.bodyUsed,response.body!.locked);
    await failure('locked text',()=>response.text());await failure('locked clone',()=>response.clone());await failure('locked reader',()=>response.body!.getReader());
    reader.releaseLock();console.log('unlock',response.bodyUsed,response.body!.locked,await response.text());
    const cancelled=new Request('https://example.com/',{method:'POST',body:'cancel'});const cancellation=cancelled.body!.getReader({mode:'byob'});await cancellation.cancel('stop');
    const result=await cancellation.read(new Uint8Array(4));console.log('cancel',cancelled.bodyUsed,result.done,result.value!.length);cancellation.releaseLock();
    const partial=new Response('abcde');const byob=partial.body!.getReader({mode:'byob'});await byob.read(new Uint8Array(2));byob.releaseLock();
    const branches=partial.body!.tee();const a=branches[0].getReader({mode:'byob'});
    console.log('tee remainder',Array.from((await a.read(new Uint8Array(8))).value!).join(','),await new Response(branches[1]).text());
    const min=new Response('abc');const minReader=min.body!.getReader({mode:'byob'});await minReader.read(new Uint8Array(1));
    await failure('remaining minimum',()=>minReader.read(new Uint8Array(8),{min:4}));
    const untouched=new Response('safe');const safe=untouched.body!.getReader({mode:'byob'});
    await failure('bad view',()=>safe.read(new Uint8Array(0)));console.log('invalid unused',untouched.bodyUsed);safe.releaseLock();console.log('still readable',await untouched.text());
    const shared=new Response('shared');const sharedReader=shared.body!.getReader({mode:'byob'});
    await failure('shared view',()=>sharedReader.read(new Uint8Array(new SharedArrayBuffer(4)) as any));console.log('shared unused',shared.bodyUsed);sharedReader.releaseLock();
    const reentrant=new Response('reentrant');const reentrantReader=reentrant.body!.getReader({mode:'byob'});
    await failure('release in options',()=>reentrantReader.read(new Uint8Array(4),{get min(){reentrantReader.releaseLock();return 1;}}));
    console.log('reentrant unused',reentrant.bodyUsed,await reentrant.text());
}
main();
