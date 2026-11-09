export {};
async function compress(format: CompressionFormat): Promise<Uint8Array> {
    const source=new ReadableStream<BufferSource>({start(controller){controller.enqueue(new TextEncoder().encode('hello world'.repeat(10)));controller.close();}});
    const bytes:number[]=[];await source.pipeThrough(new CompressionStream(format)).pipeTo(new WritableStream<Uint8Array>({write(chunk){bytes.push(...chunk);}}));
    return new Uint8Array(bytes);
}
async function decode(format: CompressionFormat,bytes: Uint8Array): Promise<void> {
    const source=new ReadableStream<BufferSource>({start(controller){controller.enqueue(bytes as BufferSource);controller.close();}});
    await source.pipeThrough(new DecompressionStream(format)).pipeTo(new WritableStream());
}
async function main(): Promise<void> {
    for(const name of ['gzip','deflate','deflate-raw','brotli']) {
        const format=name as CompressionFormat,valid=await compress(format);
        for(const [kind,bytes] of [['empty',new Uint8Array()],['truncated',valid.slice(0,-1)],['trailing',new Uint8Array([...valid,255])]] as [string,Uint8Array][]) {
            try{await decode(format,bytes);console.log('accepted',format,kind);}catch(error){console.log('rejected',format,kind,(error as Error).name);}
        }
        if(format==='gzip'||format==='deflate') {
            const bad=valid.slice();bad[bad.length-1]^=1;
            try{await decode(format,bad);}catch(error){console.log('checksum',format,(error as Error).name);}
        }
    }
    for(const value of [undefined,null,{}]) {
        const stream=new DecompressionStream('gzip'),writer=stream.writable.getWriter(),reader=stream.readable.getReader();
        const reading=reader.read().catch(error=>error.name);
        try{await writer.write(value as any);}catch(error){console.log('input error',(error as Error).name);}
        console.log('read error',await reading);
    }
    const aborted=new DecompressionStream('gzip'),writer=aborted.writable.getWriter();
    const writing=writer.write(new Uint8Array([31])).catch(error=>error);await writer.abort('stop');console.log('aborted',await writing);
}
main().catch(error=>console.log('failure',String(error)));
