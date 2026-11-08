export {};
async function main(): Promise<void> {
    let active = 0;
    const sink: UnderlyingSink<number> = {
        start(controller) {console.log('start', this === sink, controller instanceof WritableStreamDefaultController, controller.signal.aborted);},
        async write(value,controller) {active++;console.log('write', value, active, this === sink, controller.signal.aborted);await Promise.resolve();active--;},
        close() {console.log('sink close', this === sink);}
    };
    const stream = new WritableStream(sink,new CountQueuingStrategy({highWaterMark: 2}));
    console.log('stream', stream instanceof WritableStream, stream.locked, Object.prototype.toString.call(stream));
    const writer = new WritableStreamDefaultWriter(stream);
    console.log('writer', stream.locked, writer instanceof WritableStreamDefaultWriter, writer.desiredSize);
    const first = writer.write(1),second = writer.write(2);
    console.log('queued', writer.desiredSize, writer.ready === writer.ready);
    const close = writer.close();
    console.log('closing', writer.desiredSize);
    console.log('results', await first, await second, await close, await writer.closed, writer.desiredSize);
    writer.releaseLock();console.log('released', stream.locked);
    try {await writer.write(3);} catch (error) {console.log('released write', (error as Error).name);}

    let finish!: () => void;
    const pressured = new WritableStream<number>({write() {return new Promise<void>(resolve => {finish = resolve;});}});
    const pressureWriter = pressured.getWriter();const initialReady = pressureWriter.ready;
    const writing = pressureWriter.write(4);let ready = false;
    const waiting = pressureWriter.ready.then(() => {ready = true;});
    await Promise.resolve();console.log('backpressure', pressureWriter.desiredSize, ready, initialReady !== pressureWriter.ready);
    finish();await writing;await waiting;
    console.log('ready', pressureWriter.desiredSize, ready);
    await pressureWriter.close();pressureWriter.releaseLock();

    const unlocked = new WritableStream();
    const held = unlocked.getWriter();
    try {unlocked.getWriter();} catch (error) {console.log('second writer', (error as Error).name);}
    try {await unlocked.close();} catch (error) {console.log('locked close', (error as Error).name);}
    try {await unlocked.abort();} catch (error) {console.log('locked abort', (error as Error).name);}
    held.releaseLock();console.log('unlocked close', await unlocked.close());

    let controller!: WritableStreamDefaultController;
    const aborted = new WritableStream({start(value) {controller = value;},abort(reason) {console.log('sink abort', reason);}});
    const abortWriter = aborted.getWriter();
    controller.signal.addEventListener('abort',() => console.log('signal', controller.signal.aborted, controller.signal.reason));
    console.log('abort result', await abortWriter.abort('stop'));
    try {await abortWriter.closed;} catch (error) {console.log('abort closed', error);}
    console.log('abort size', abortWriter.desiredSize);
    abortWriter.releaseLock();

    let byteController!: WritableStreamDefaultController;
    const byteStream = new WritableStream<Uint8Array>({start(value) {byteController = value;},write() {}},new ByteLengthQueuingStrategy({highWaterMark: 10}));
    const byteWriter = byteStream.getWriter();const bytes = byteWriter.write(new Uint8Array(4));
    console.log('byte pressure', byteWriter.desiredSize);
    await bytes;console.log('byte ready', byteWriter.desiredSize, byteController.signal.aborted);
    await byteWriter.close();

    class Child extends WritableStream<number> {field = 7;}
    const child = new Child();
    console.log('subclass', child instanceof Child, child instanceof WritableStream, child.field);await child.close();
    console.log('metadata', WritableStream.length, WritableStreamDefaultWriter.length, WritableStreamDefaultController.length);
}
main().catch(error => console.log('failure', String(error)));
