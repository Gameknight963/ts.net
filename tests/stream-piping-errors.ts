export {};
async function main(): Promise<void> {
    for (const preventAbort of [false,true]) {
        let aborted: unknown = 'none';
        const source = new ReadableStream({start(controller) {controller.error('source');}});
        const destination = new WritableStream({abort(reason) {aborted = reason;}});
        try {await source.pipeTo(destination,{preventAbort});} catch (error) {console.log('source error', preventAbort, error, aborted, source.locked, destination.locked);}
    }
    for (const preventCancel of [false,true]) {
        let cancelled: unknown = 'none';
        const source = new ReadableStream<number>({start(controller) {controller.enqueue(1);},cancel(reason) {cancelled = reason;}});
        const destination = new WritableStream({write() {throw 'destination';}});
        try {await source.pipeTo(destination,{preventCancel});} catch (error) {console.log('destination error', preventCancel, error, cancelled, source.locked, destination.locked);}
    }
    const source = new ReadableStream({start(controller) {controller.error('original');}});
    const abortFailure = new WritableStream({abort() {throw 'abort failure';}});
    try {await source.pipeTo(abortFailure);} catch (error) {console.log('abort override', error);}
    const cancelFailure = new ReadableStream({cancel() {throw 'cancel failure';}});
    const failed = new WritableStream({start(controller) {controller.error('destination');}});
    try {await cancelFailure.pipeTo(failed);} catch (error) {console.log('cancel override', error);}

    const controller = new AbortController();controller.abort('signal');
    let abortReason: unknown,cancelReason: unknown;
    const abortedSource = new ReadableStream({cancel(reason) {cancelReason = reason;}});
    const abortedDestination = new WritableStream({abort(reason) {abortReason = reason;}});
    try {await abortedSource.pipeTo(abortedDestination,{signal: controller.signal});} catch (error) {console.log('already aborted', error, abortReason, cancelReason, abortedSource.locked, abortedDestination.locked);}

    const closed = new WritableStream();await closed.close();
    let cancelled = false;
    const open = new ReadableStream({cancel() {cancelled = true;}});
    try {await open.pipeTo(closed);} catch (error) {console.log('closed destination', (error as Error).name, cancelled, open.locked);}
    const locked = new ReadableStream();locked.getReader();
    try {await locked.pipeTo(new WritableStream());} catch (error) {console.log('locked source', (error as Error).name);}
    const lockedSink = new WritableStream();lockedSink.getWriter();
    try {await new ReadableStream().pipeTo(lockedSink);} catch (error) {console.log('locked sink', (error as Error).name);}
    try {await new ReadableStream().pipeTo({} as WritableStream);} catch (error) {console.log('destination brand', (error as Error).name);}
    try {new ReadableStream().pipeThrough({readable: {} as ReadableStream,writable: new WritableStream()});} catch (error) {console.log('through brand', (error as Error).name);}
}
main().catch(error => console.log('failure', String(error)));
