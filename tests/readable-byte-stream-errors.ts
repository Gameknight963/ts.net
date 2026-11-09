export {};
async function failure(label:string,action:()=>any):Promise<void>{try{await action();console.log(label,'unexpected');}catch(error){console.log(label,(error as Error).name);}}
async function main():Promise<void>{
    await failure('constructor',()=>new ReadableByteStreamController());
    for(const type of [null,'invalid',5])await failure('type',()=>new ReadableStream({type} as any));
    for(const size of [null,1,()=>1])await failure('size',()=>new ReadableStream({type:'bytes'},{size} as any));
    for(const highWaterMark of [-1,NaN])await failure('hwm',()=>new ReadableStream({type:'bytes'},{highWaterMark}));
    let controller!:ReadableByteStreamController;
    const stream=new ReadableStream({type:'bytes',start(c){controller=c;}});
    for(const chunk of [undefined,null,1,'text',new ArrayBuffer(1),new Uint8Array(0),new DataView(new ArrayBuffer(0)),new Uint8Array(new SharedArrayBuffer(2))]){
        await failure('chunk',()=>controller.enqueue(chunk as any));
    }
    const buffer=new ArrayBuffer(2);const detached=new Uint8Array(buffer);structuredClone(buffer,{transfer:[buffer]});
    await failure('detached',()=>controller.enqueue(detached));
    const reader=stream.getReader();const pending=reader.read();const reason={error:'exact'};controller.error(reason);
    try{await pending;}catch(error){console.log('error',error===reason,controller.desiredSize===null);}
    await failure('enqueue errored',()=>controller.enqueue(new Uint8Array([1])));reader.releaseLock();
    let closing!:ReadableByteStreamController;new ReadableStream({type:'bytes',start(c){closing=c;c.close();}});
    const intact=new Uint8Array([1]);await failure('enqueue closed',()=>closing.enqueue(intact));console.log('still attached',intact.byteLength);
    await failure('close twice',()=>closing.close());
    await failure('brand',()=>ReadableByteStreamController.prototype.enqueue.call({},new Uint8Array([1])));
}
main();
