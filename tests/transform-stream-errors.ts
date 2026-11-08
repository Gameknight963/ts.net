export {};
async function main(): Promise<void> {
    for (const mode of ['throw','reject','controller']) {
        const stream = new TransformStream<number,number>({transform(_,controller) {
            if(mode==='throw')throw 'transform';if(mode==='reject')return Promise.reject('transform');controller.error('transform');
        }});
        const writer = stream.writable.getWriter(),reader = stream.readable.getReader();
        const reading = reader.read().catch(error => error);
        console.log('write', mode, await writer.write(1).then(() => 'fulfilled',error => error));
        console.log('read', await reading);
        try {await writer.closed;} catch (error) {console.log('writer error', error);}
    }
    const flush = new TransformStream({flush() {throw 'flush';}});
    const reader = flush.readable.getReader();const reading = reader.read().catch(error => error);
    try {await flush.writable.close();} catch (error) {console.log('flush close', error);}
    console.log('flush read', await reading);

    let controller!: TransformStreamDefaultController;
    const terminated = new TransformStream({start(value) {controller = value;}});
    const terminalWriter = terminated.writable.getWriter();
    const waiting = terminalWriter.write(1).catch(error => error.name);controller.terminate();
    console.log('terminate', await waiting, (await terminated.readable.getReader().read()).done);

    const aborted = new TransformStream();const abortWriter = aborted.writable.getWriter();
    const write = abortWriter.write(1).catch(error => error);
    await Promise.resolve();const abort = abortWriter.abort('abort');
    console.log('abort pending', await write, await abort);
    try {await aborted.readable.getReader().read();} catch (error) {console.log('abort read', error);}

    let cancellations = 0;
    const cancelled = new TransformStream({cancel(reason: unknown) {cancellations++;console.log('cancel callback', reason);}} as any);
    const cancelWriter = cancelled.writable.getWriter();const cancelWrite = cancelWriter.write(1).catch(error => error);
    await cancelled.readable.cancel('cancel');
    console.log('cancel pending', await cancelWrite, cancellations);
    for (const transformer of [{readableType:'bytes'},{writableType:'bytes'},{transform:3},{flush:3},{start:3}]) {
        try {new TransformStream(transformer as any);} catch (error) {console.log('invalid', (error as Error).name);}
    }
}
main().catch(error => console.log('failure', String(error)));
