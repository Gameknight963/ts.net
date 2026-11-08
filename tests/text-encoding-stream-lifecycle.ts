export {};
async function main(): Promise<void> {
    for (const Constructor of [TextEncoderStream,TextDecoderStream]) {
        const stream = new Constructor();
        console.log('constructor', Constructor.name, stream.constructor === Constructor);
        for (const key of ['encoding','readable','writable']) {
            const descriptor = Object.getOwnPropertyDescriptor(Constructor.prototype,key)!;
            console.log('getter', key, descriptor.enumerable, descriptor.configurable, descriptor.set === undefined);
            try {descriptor.get!.call({});} catch (error) {console.log('brand', (error as Error).name);}
        }
        try {(Constructor as any)();} catch (error) {console.log('requires new', (error as Error).name);}
        await stream.readable.cancel('stop');
    }
    const detached = new ArrayBuffer(4);(detached as any).transfer();
    const decoder = new TextDecoderStream();const writer = decoder.writable.getWriter(),reader = decoder.readable.getReader();
    const reading = reader.read();
    console.log('detached write', await writer.write(detached));await writer.close();
    console.log('detached read', (await reading).done);

    const encoder = new TextEncoderStream();const encoderWriter = encoder.writable.getWriter(),encoderReader = encoder.readable.getReader();
    const pending = encoderReader.read().catch(error => error);
    await encoderWriter.write('\ud800');await encoderWriter.abort('abort');
    console.log('abort buffered surrogate', await pending);

    const failure = {message: 'coercion'};
    const bad = new TextEncoderStream();const badWriter = bad.writable.getWriter(),badReader = bad.readable.getReader();
    const badRead = badReader.read().catch(error => error);
    try {await badWriter.write({toString() {throw failure;}} as any);} catch (error) {console.log('coercion write', error === failure);}
    console.log('coercion read', (await badRead) === failure);
}
main().catch(error => console.log('failure', String(error)));
