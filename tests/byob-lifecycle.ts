export {};
async function main():Promise<void>{
    let controller!:ReadableByteStreamController;
    const stream=new ReadableStream({type:'bytes',start(c){controller=c;}});
    const reader=stream.getReader({mode:'byob'});const first=reader.read(new Uint8Array(8)).catch(error=>error.name);
    const second=reader.read(new Uint8Array(4)).catch(error=>error.name);const request=controller.byobRequest!;
    reader.releaseLock();console.log('released',await first,await second,stream.locked);
    (request.view as Uint8Array)[0]=42;request.respond(1);const next=stream.getReader();const read=next.read();
    console.log('released bytes',Array.from((await read).value!).join(','));controller.close();next.releaseLock();
    let cancelled:any;
    const cancelling=new ReadableStream({type:'bytes',cancel(reason){cancelled=reason;}}).getReader({mode:'byob'});
    const pending=cancelling.read(new Uint8Array(4));const reason={exact:1};await cancelling.cancel(reason);
    const closed=await pending;console.log('cancel',cancelled===reason,closed.done,closed.value===undefined);
    let partial!:ReadableByteStreamController;
    const partialReader=new ReadableStream({type:'bytes',start(c){partial=c;}}).getReader({mode:'byob'});
    const partialRead=partialReader.read(new Uint16Array(2)).catch(error=>error.name);partial.enqueue(new Uint8Array([1]));
    try{partial.close();}catch(error){console.log('alignment',(error as Error).name);}
    console.log('alignment read',await partialRead);
    let end!:ReadableByteStreamController;
    const endReader=new ReadableStream({type:'bytes',start(c){end=c;}}).getReader({mode:'byob'});
    const ending=endReader.read(new Uint8Array(8),{min:4});end.enqueue(new Uint8Array([1,2]));const endRequest=end.byobRequest!;end.close();endRequest.respond(0);
    const ended=await ending;console.log('partial close',ended.done,Array.from(ended.value!).join(','));
    const tee=new ReadableStream({type:'bytes',start(c){c.enqueue(new Uint8Array([3,4,5]));c.close();}}).tee();
    for(const branch of tee){const r=branch.getReader({mode:'byob'});console.log('tee',Array.from((await r.read(new Uint8Array(8))).value!).join(','),(await r.read(new Uint8Array(8))).done);}
    let auto!:ReadableByteStreamController;
    const allocated=new ReadableStream({type:'bytes',autoAllocateChunkSize:4,start(c){auto=c;}});
    const defaultReader=allocated.getReader();const allocation=defaultReader.read();const old=auto.byobRequest!;auto.enqueue(new Uint8Array([9,10]));
    const chunk=(await allocation).value!;console.log('enqueue allocation',Array.from(chunk).join(','),chunk.buffer.byteLength,old.view===null);auto.close();
}
main();
