export {};
async function main():Promise<void>{
    let controller!:ReadableByteStreamController;
    const stream=new ReadableStream<Uint8Array>({type:'bytes',start(c){controller=c as ReadableByteStreamController;}});
    console.log('initial',controller instanceof ReadableByteStreamController,Object.prototype.toString.call(controller),controller.desiredSize,controller.byobRequest===null);
    const input=new Uint8Array([1,2,3]);controller.enqueue(input);
    console.log('transfer',input.byteLength,input.buffer.byteLength,controller.desiredSize);
    const reader=stream.getReader();const first=await reader.read();
    console.log('read',first.done,Array.from(first.value!).join(','),controller.desiredSize,first.value instanceof Uint8Array);
    const buffer=new ArrayBuffer(8);new Uint8Array(buffer).set([0,9,8,7,6,0]);
    controller.enqueue(new DataView(buffer,1,4));console.log('view transfer',buffer.byteLength);
    controller.close();const second=await reader.read();
    console.log('view',second.value!.byteOffset,second.value!.buffer.byteLength,Array.from(second.value!).join(','));
    console.log('closed',(await reader.read()).done,controller.desiredSize);await reader.closed;reader.releaseLock();
    let pulls=0;
    const pulling=new ReadableStream<Uint8Array>({type:'bytes',pull(c){pulls++;c.enqueue(new Uint8Array([65]));if(pulls===2)c.close();}});
    await Promise.resolve();await Promise.resolve();console.log('no eager pull',pulls);
    console.log('response',await new Response(pulling).text(),pulls);
    let sizes!:ReadableByteStreamController;
    const queued=new ReadableStream({type:'bytes',start(c){sizes=c;}},{highWaterMark:5});
    console.log('hwm',sizes.desiredSize);sizes.enqueue(new Uint16Array([0x1234]));console.log('bytes size',sizes.desiredSize);
    await queued.cancel();console.log('cancel size',sizes.desiredSize);
    class Custom extends ReadableStream<Uint8Array>{label='custom';}
    const custom=new Custom({type:'bytes',start(c){c.enqueue(new Uint8Array([90]));c.close();}});
    console.log('subclass',custom instanceof Custom,await new Response(custom).text());
    const teeSource=new ReadableStream<Uint8Array>({type:'bytes',start(c){c.enqueue(new Uint8Array([1,2]));c.close();}});
    const branches=teeSource.tee();const left=branches[0].getReader(),right=branches[1].getReader();
    const a=(await left.read()).value!,b=(await right.read()).value!;a[0]=99;
    console.log('tee copies',a===b,a.buffer===b.buffer,b[0],(await left.read()).done,(await right.read()).done);
    console.log('controller',ReadableByteStreamController.length,ReadableByteStreamController.prototype.enqueue.length,ReadableByteStreamController.prototype.close.length);
}
main();
