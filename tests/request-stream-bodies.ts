export {};
function stream(): ReadableStream<Uint8Array> {return new ReadableStream({start(c){c.enqueue(new Uint8Array([65,66]));c.close();}});}
function create(body: ReadableStream<any>, extra: any = {}): Request {return new Request('https://example.com/',{method:'POST',body,duplex:'half',...extra} as any);}
async function failure(label:string,action:()=>any):Promise<void>{try{await action();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
async function main():Promise<void>{
    const body=stream();const request=create(body);
    console.log('initial',request.body===body,request.bodyUsed,request.headers.get('content-type'));
    const copy=request.clone();console.log('clone',request.body!==body,request.body!==copy.body,request.bodyUsed,copy.bodyUsed);
    console.log('read',await request.text(),await copy.text());
    await failure('read twice',()=>request.text());await failure('clone used',()=>request.clone());
    const source=create(stream());const old=source.body;const moved=new Request(source);
    console.log('moved',moved.body!==old,source.bodyUsed,source.body!.locked,moved.bodyUsed);
    console.log('moved text',await moved.text());
    await failure('source used',()=>source.text());
    const preserved=create(stream());
    await failure('invalid transfer',()=>new Request(preserved,{method:'GET'}));
    console.log('preserved',preserved.bodyUsed,preserved.body!.locked,await preserved.text());
    const overridden=create(stream());const replacement=new Request(overridden,{body:'replacement'});
    console.log('override',overridden.bodyUsed,overridden.body!.locked,await overridden.text(),await replacement.text());
    await failure('duplex',()=>new Request('https://example.com/',{method:'POST',body:stream()}));
    await failure('keepalive',()=>create(stream(),{keepalive:true}));
    await failure('no cors',()=>create(stream(),{mode:'no-cors'}));
    await failure('GET',()=>create(stream(),{method:'GET'}));
    const locked=stream();const reader=locked.getReader();await failure('locked',()=>create(locked));reader.releaseLock();
    await locked.cancel();await failure('disturbed',()=>create(locked));
    const reason={error:'stream'};
    const error=create(new ReadableStream({start(c){c.error(reason);}}));
    try{await error.text();}catch(value){console.log('error identity',value===reason,error.bodyUsed);}
    await failure('invalid chunk',()=>create(new ReadableStream({start(c){c.enqueue('bad');c.close();}})).text());
}
main();
