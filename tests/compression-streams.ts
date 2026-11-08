export {};
async function main(): Promise<void> {
    for (const format of ['gzip','deflate','deflate-raw','brotli']) {
        const stream = new CompressionStream(format as CompressionFormat);
        const results: number[] = [];
        const piping = stream.readable.pipeTo(new WritableStream<Uint8Array>({write(value) {if(value.length===0)throw 'empty output';results.push(...value);}}));
        const writer = stream.writable.getWriter();
        await writer.write(new TextEncoder().encode('hello'));
        await writer.write(new Uint8Array([0,1,2]));await writer.close();await piping;
        console.log('format', format, results.length>0, stream.readable.locked, stream.writable.locked);
        if(format==='gzip')console.log('gzip header',results[0],results[1]);
        if(format==='deflate')console.log('zlib header',((results[0]<<8)+results[1])%31);
        writer.releaseLock();
    }
    for(const format of [undefined,null,'GZIP','bad',3,Symbol('format')]) {
        try {new CompressionStream(format as any);} catch(error) {console.log('invalid format',(error as Error).name);}
    }
    let gets=0;
    const coerced=new CompressionStream({toString(){gets++;return 'gzip';}} as any);console.log('coercion',gets);await coerced.readable.cancel();
    for(const input of [undefined,null,{}]) {
        const stream=new CompressionStream('gzip');const writer=stream.writable.getWriter(),reader=stream.readable.getReader();
        const reading=reader.read().catch(error=>error.name);
        try{await writer.write(input as any);}catch(error){console.log('invalid input',(error as Error).name);}
        console.log('invalid read',await reading);
    }
    const aborted=new CompressionStream('gzip');const writer=aborted.writable.getWriter();const pending=writer.write(new Uint8Array([1])).catch(error=>error);
    await writer.abort('stop');console.log('abort',await pending);
    class Child extends CompressionStream {field=7;}
    const child=new Child('gzip');console.log('subclass',child instanceof Child,child instanceof CompressionStream,child.field);await child.readable.cancel();
    console.log('metadata',CompressionStream.name,CompressionStream.length);
}
main().catch(error=>console.log('failure',String(error)));
