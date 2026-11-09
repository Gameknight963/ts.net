export {};
async function failure(label:string,action:()=>any):Promise<void>{try{await action();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
async function main():Promise<void>{
    await failure('new reader',()=>new ReadableStreamBYOBReader(new ReadableStream()));
    await failure('new request',()=>new ReadableStreamBYOBRequest());
    const stream=new ReadableStream({type:'bytes'});const reader=stream.getReader({mode:'byob'});
    await failure('locked',()=>stream.getReader());await failure('locked byob',()=>stream.getReader({mode:'byob'}));
    for(const value of [undefined,null,{},new ArrayBuffer(4),new Uint8Array(0),new DataView(new ArrayBuffer(0)),new Uint8Array(new SharedArrayBuffer(4))])await failure('view',()=>reader.read(value as any));
    for(const min of [0,-1,NaN,Infinity,9])await failure('minimum',()=>reader.read(new Uint8Array(8),{min}));
    reader.releaseLock();await failure('released',()=>reader.read(new Uint8Array(1)));await failure('released cancel',()=>reader.cancel());
    let controller!:ReadableByteStreamController;
    const source=new ReadableStream({type:'bytes',start(c){controller=c;}});const active=source.getReader({mode:'byob'});
    const pending=active.read(new Uint8Array(4));const request=controller.byobRequest!;
    for(const count of [0,5,NaN])await failure('respond',()=>request.respond(count));
    await failure('replacement offset',()=>request.respondWithNewView(new Uint8Array(new ArrayBuffer(4),1,1)));
    await failure('replacement capacity',()=>request.respondWithNewView(new Uint8Array(8)));
    (request.view as Uint8Array)[0]=7;request.respond(1);console.log('valid after errors',(await pending).value![0]);
    await failure('stale',()=>request.respond(1));await failure('stale view',()=>request.respondWithNewView(new Uint8Array(4)));controller.close();active.releaseLock();
    await failure('brand',()=>ReadableStreamBYOBReader.prototype.read.call({},new Uint8Array(1)));
    await failure('request brand',()=>ReadableStreamBYOBRequest.prototype.respond.call({},1));
    const reason={exact:true};const errored=new ReadableStream({type:'bytes',start(c){c.error(reason);}}).getReader({mode:'byob'});
    const intact=new Uint8Array(2);try{await errored.read(intact);}catch(error){console.log('errored',error===reason,intact.byteLength);}
}
main();
