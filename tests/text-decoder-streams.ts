export {};
async function decode(chunks: ArrayBufferView[], label = 'utf-8', options?: TextDecoderOptions): Promise<string> {
    const results: string[] = [];
    const source = new ReadableStream<BufferSource>({start(controller) {for(const chunk of chunks)controller.enqueue(chunk as BufferSource);controller.close();}});
    await source.pipeThrough(new TextDecoderStream(label,options)).pipeTo(new WritableStream<string>({write(value) {results.push(value);}}));
    return results.map(value => JSON.stringify(value)).join('|');
}
async function main(): Promise<void> {
    const bytes = (values: number[]): Uint8Array => new Uint8Array(values);
    console.log('utf8 split', await decode([bytes([65,0xf0]),bytes([0x9f]),bytes([0x98,0x80,66])]));
    for (const ignoreBOM of [false,true]) {
        console.log('bom', ignoreBOM, await decode([bytes([0xef]),bytes([0xbb]),bytes([0xbf,65]),bytes([0xef,0xbb,0xbf])],'utf-8',{ignoreBOM}));
    }
    console.log('truncated', await decode([bytes([65,0xe2,0x82])]));
    console.log('utf16 split', await decode([bytes([0xff]),bytes([0xfe,0x3d,0xd8]),bytes([0x00]),bytes([0xde])],'utf-16le'));
    console.log('legacy', await decode([bytes([0x80,65])],'windows-1252'));
    const backing = bytes([0,65,66,0]);console.log('view', await decode([new DataView(backing.buffer,1,2)]));
    const shared = new Uint8Array(new SharedArrayBuffer(2));shared.set([67,68]);console.log('shared', await decode([shared]));

    for (const chunks of [[bytes([0xff])],[bytes([0xe2,0x82])]]) {
        try {await decode(chunks,'utf-8',{fatal:true});} catch (error) {console.log('fatal', (error as Error).name);}
    }
    for (const input of [undefined,null,'text',{}]) {
        const stream = new TextDecoderStream();const writer = stream.writable.getWriter(),reader = stream.readable.getReader();
        const reading = reader.read().catch(error => error.name);
        try {await writer.write(input as any);} catch (error) {console.log('invalid write', (error as Error).name);}
        console.log('invalid read', await reading);
    }
    for (const label of ['bad-encoding','replacement']) {
        try {new TextDecoderStream(label);} catch (error) {console.log('label', (error as Error).name);}
    }
}
main().catch(error => console.log('failure', String(error)));
