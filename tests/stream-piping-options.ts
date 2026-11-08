export {};
async function main(): Promise<void> {
    for (const preventAbort of [false,true]) for (const preventCancel of [false,true]) {
        const signal = new AbortController();signal.abort('reason');
        let aborted = 0,cancelled = 0;
        const source = new ReadableStream({cancel() {cancelled++;}});
        const sink = new WritableStream({abort() {aborted++;}});
        try {await source.pipeTo(sink,{signal: signal.signal,preventAbort,preventCancel});} catch (error) {
            console.log('prevention', preventAbort, preventCancel, error, aborted, cancelled, source.locked, sink.locked);
        }
    }
    let pendingController!: ReadableStreamDefaultController;
    const pending = new ReadableStream({start(controller) {pendingController = controller;}});
    const destination = new WritableStream();const controller = new AbortController();
    const pipe = pending.pipeTo(destination,{signal: controller.signal,preventCancel: true,preventAbort: true}).catch(error => error);
    await Promise.resolve();controller.abort('stop');
    console.log('pending abort', await pipe, pending.locked, destination.locked);
    pendingController.enqueue(1);pendingController.close();
    console.log('source reusable', (await pending.getReader().read()).value);
    await destination.close();

    let trace = '';
    const options = {
        get preventAbort() {trace += 'abort|';return false;},
        get preventCancel() {trace += 'cancel|';return false;},
        get preventClose() {trace += 'close|';return false;},
        get signal() {trace += 'signal|';return undefined;}
    };
    const source = new ReadableStream({start(controller) {controller.close();}});
    await source.pipeTo(new WritableStream(),options);console.log('options', trace);
    trace = '';
    try {await ReadableStream.prototype.pipeTo.call({} as ReadableStream,new WritableStream(),options);} catch (error) {console.log('source brand', (error as Error).name, trace);}
    try {await new ReadableStream().pipeTo({} as WritableStream,options);} catch (error) {console.log('sink brand', (error as Error).name, trace);}
    for (const signal of [null,{},3]) {
        try {await new ReadableStream().pipeTo(new WritableStream(),{signal} as any);} catch (error) {console.log('signal brand', (error as Error).name);}
    }
    console.log('metadata', ReadableStream.prototype.pipeTo.length, ReadableStream.prototype.pipeThrough.length);
}
main().catch(error => console.log('failure', String(error)));
