export {};
function queued():ReadableStream<Uint8Array<ArrayBuffer>>{return new ReadableStream({type:'bytes',start(c){c.enqueue(new Uint8Array([1,2,3,4,5]));c.close();}});}
async function main():Promise<void>{
    const stream=queued();const reader=stream.getReader({mode:'byob'});
    console.log('reader',reader instanceof ReadableStreamBYOBReader,stream.locked,Object.prototype.toString.call(reader));
    const input=new Uint16Array(2);const pending=reader.read(input);console.log('detached',input.byteLength);
    const first=await pending;console.log('typed',first.done,first.value!.constructor===Uint16Array,Array.from(first.value!).join(','),first.value!.buffer.byteLength);
    const second=await reader.read(new Uint8Array(4));console.log('remainder',second.done,Array.from(second.value!).join(','));
    const last=await reader.read(new Uint8Array(4));console.log('closed',last.done,last.value!.byteLength);await reader.closed;reader.releaseLock();console.log('unlocked',stream.locked);
    const viewReader=new ReadableStream({type:'bytes',start(c){c.enqueue(new Uint8Array([7,8,9]));c.close();}}).getReader({mode:'byob'});
    const view=await viewReader.read(new DataView(new ArrayBuffer(8),2,4));
    console.log('data view',view.value instanceof DataView,view.value!.byteOffset,view.value!.byteLength,view.value!.getUint8(0));
    let pulls=0;
    const minimum=new ReadableStream({type:'bytes',pull(c){pulls++;c.enqueue(new Uint8Array([pulls,pulls]));if(pulls===2)c.close();}}).getReader({mode:'byob'});
    const filled=await minimum.read(new Uint8Array(8),{min:4});console.log('minimum',Array.from(filled.value!).join(','),pulls);
    const directStream=queued();const direct=new ReadableStreamBYOBReader(directStream);console.log('direct',directStream.locked,Array.from((await direct.read(new Uint8Array(8))).value!).join(','));direct.releaseLock();
    class Custom extends ReadableStreamBYOBReader{label='custom';}
    const subclass=new Custom(queued());console.log('subclass',subclass instanceof Custom,subclass.label,(await subclass.read(new Uint8Array(8))).value!.byteLength);subclass.releaseLock();
    console.log('metadata',ReadableStreamBYOBReader.length,ReadableStreamBYOBReader.prototype.read.length,ReadableStreamBYOBRequest.length,ReadableStreamBYOBRequest.prototype.respond.length);
}
main();
