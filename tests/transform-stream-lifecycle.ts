export {};
async function main(): Promise<void> {
    let controller!: TransformStreamDefaultController<number>;
    const queued = new TransformStream<number,number>({start(value) {controller = value;value.enqueue(1);value.enqueue(2);}});
    controller.terminate();const reader = queued.readable.getReader();
    console.log('terminate queue', (await reader.read()).value, (await reader.read()).value, (await reader.read()).done);
    try {await queued.writable.getWriter().write(3);} catch (error) {console.log('terminate write', (error as Error).name);}

    const inside = new TransformStream<number,number>({transform(value,control) {control.enqueue(value);control.terminate();}});
    const insideWriter = inside.writable.getWriter(),insideReader = inside.readable.getReader();
    const written = insideWriter.write(4);
    console.log('terminate transform', (await insideReader.read()).value, await written, (await insideReader.read()).done);

    let ready!: () => void;
    let calls = 0;
    const delayed = new TransformStream<number,number>({start() {return new Promise<void>(resolve => {ready = resolve;});},transform(value,control) {calls++;control.enqueue(value);}});
    const delayedWriter = delayed.writable.getWriter(),delayedReader = delayed.readable.getReader();
    const writing = delayedWriter.write(5),reading = delayedReader.read();
    await Promise.resolve();console.log('start wait', calls);ready();
    console.log('start done', (await reading).value, await writing, calls);await delayedWriter.close();

    const rejectedStart = new TransformStream({start() {return Promise.reject('start');}});
    try {await rejectedStart.readable.getReader().read();} catch (error) {console.log('start read', error);}
    try {await rejectedStart.writable.getWriter().closed;} catch (error) {console.log('start writer', error);}

    const failedCancel = new TransformStream({cancel() {throw 'cancel failure';}} as any);
    const failedWriter = failedCancel.writable.getWriter();
    try {await failedCancel.readable.cancel('reason');} catch (error) {console.log('cancel rejection', error);}
    try {await failedWriter.closed;} catch (error) {console.log('cancel writer', error);}

    let frozenController!: TransformStreamDefaultController<number>;
    const frozen = new TransformStream<number,number>({start(value) {frozenController = value;}});
    Object.freeze(frozen);Object.freeze(frozenController);
    const frozenReader = frozen.readable.getReader(),frozenWriter = frozen.writable.getWriter();
    const read = frozenReader.read();const write = frozenWriter.write(6);
    console.log('frozen', (await read).value, await write, Object.isFrozen(frozen));await frozenWriter.close();

    let finishFlush!: () => void;
    const flushing = new TransformStream({flush() {return new Promise<void>(resolve => {finishFlush = resolve;});}});
    const flushWriter = flushing.writable.getWriter();const closing = flushWriter.close();
    for(let tick=0;tick<5;tick++)await Promise.resolve();
    const cancelling = flushing.readable.cancel('stop');finishFlush();
    console.log('cancel during flush', await closing, await cancelling, await flushWriter.closed);

    let finishCancel!: () => void;
    const cancellingStream = new TransformStream({cancel() {return new Promise<void>(resolve => {finishCancel = resolve;});}} as any);
    const cancelWriter = cancellingStream.writable.getWriter();const pendingWrite = cancelWriter.write(7).catch(error => error);
    const cancellation = cancellingStream.readable.cancel('reason');
    let cancelClosed = false;const cancelClosedPromise = cancelWriter.closed.catch(error => {cancelClosed = true;return error;});
    await Promise.resolve();console.log('cancel waiting', cancelClosed);finishCancel();
    console.log('cancel finished', await cancellation, await pendingWrite, await cancelClosedPromise);

    let trace = '';
    const transformer = {
        get readableType() {trace += 'readable|';return undefined;},get writableType() {trace += 'writable|';return undefined;},
        get start() {trace += 'start|';return undefined;},get transform() {trace += 'transform|';return undefined;},
        get flush() {trace += 'flush|';return undefined;},get cancel() {trace += 'cancel|';return undefined;}
    };
    const writableStrategy = {get size() {trace += 'write size|';return undefined;},get highWaterMark() {trace += 'write mark|';return 1;}};
    const readableStrategy = {get size() {trace += 'read size|';return undefined;},get highWaterMark() {trace += 'read mark|';return 0;}};
    const ordered = new TransformStream(transformer,writableStrategy,readableStrategy);
    console.log('option order', trace);await ordered.readable.cancel();
}
main().catch(error => console.log('failure', String(error)));
