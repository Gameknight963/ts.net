export {};
async function collect(source: ReadableStream<Uint8Array>): Promise<number[]> {
    const values: number[] = [];
    await source.pipeTo(new WritableStream<Uint8Array>({write(chunk) {values.push(...chunk);}}));return values;
}
async function main(): Promise<void> {
    const expected = new TextEncoder().encode('hello \u20ac \ud83d\ude00\n'.repeat(20));
    for (const name of ['gzip','deflate','deflate-raw','brotli']) {
        const format = name as CompressionFormat;
        const input = new ReadableStream<BufferSource>({start(controller) {controller.enqueue(expected);controller.close();}});
        const encoded = new Uint8Array(await collect(input.pipeThrough(new CompressionStream(format))));
        for (const size of [1,2,7,31]) {
            const source = new ReadableStream<BufferSource>({start(controller) {
                for(let index=0;index<encoded.length;index+=size)controller.enqueue(encoded.slice(index,index+size));controller.close();
            }});
            const decoded = await collect(source.pipeThrough(new DecompressionStream(format)));
            console.log('roundtrip', format, size, decoded.length, decoded.every((value,index) => value===expected[index]));
        }
    }
    class Child extends DecompressionStream {field=7;}
    const child=new Child('gzip');console.log('subclass',child instanceof Child,child instanceof DecompressionStream,child.field);await child.readable.cancel();
    let calls=0;const stream=new DecompressionStream({toString(){calls++;return 'gzip';}} as any);
    console.log('metadata',DecompressionStream.name,DecompressionStream.length,calls,stream.readable instanceof ReadableStream,stream.writable instanceof WritableStream);
    await stream.readable.cancel();
    for(const format of [undefined,null,'GZIP','bad',3,Symbol('format')]) {
        try{new DecompressionStream(format as any);}catch(error){console.log('format error',(error as Error).name);}
    }
}
main().catch(error=>console.log('failure',String(error)));
