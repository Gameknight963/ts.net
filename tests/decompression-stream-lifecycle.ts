export {};
async function compressed(format: CompressionFormat): Promise<Uint8Array<ArrayBuffer>> {
    const input=new Uint8Array(50000);input.fill(65);
    const source=new ReadableStream<BufferSource>({start(controller){controller.enqueue(input);controller.close();}});
    const output:number[]=[];await source.pipeThrough(new CompressionStream(format)).pipeTo(new WritableStream<Uint8Array>({write(value){output.push(...value);}}));
    return new Uint8Array(output);
}
async function main(): Promise<void> {
    for(const name of ['gzip','deflate','deflate-raw','brotli']) {
        const format=name as CompressionFormat,data=await compressed(format),stream=new DecompressionStream(format);
        const writer=stream.writable.getWriter(),reader=stream.readable.getReader();
        const writing=writer.write(data);const first=await reader.read();
        console.log('incremental',format,first.value!.length>0,first.value!.every(value=>value===65));
        let length=first.value!.length;const closing=writer.close();
        for(;;){const item=await reader.read();if(item.done)break;length+=item.value.length;}
        console.log('complete',format,length,await writing,await closing);

        const extra=new DecompressionStream(format);const errors=extra.readable.pipeTo(new WritableStream()).catch(error=>error.name);
        const extraWriter=extra.writable.getWriter();await extraWriter.write(data);
        try{await extraWriter.write(new Uint8Array([255]));await extraWriter.close();}catch(error){console.log('later trailing',format,(error as Error).name);}
        console.log('trailing read',await errors);
    }
    const dictionary=new Uint8Array([120,187,0,0,0,1,3,0,0,0,0,1]);
    const source=new ReadableStream<BufferSource>({start(controller){controller.enqueue(dictionary);controller.close();}});
    try{await source.pipeThrough(new DecompressionStream('deflate')).pipeTo(new WritableStream());}catch(error){console.log('dictionary',(error as Error).name);}
    const frozen=new DecompressionStream('gzip');Object.freeze(frozen);
    console.log('frozen',Object.isFrozen(frozen),frozen.readable===frozen.readable,frozen.writable===frozen.writable);await frozen.readable.cancel();
}
main().catch(error=>console.log('failure',String(error)));
