export {};
async function main(): Promise<void> {
    for (const options of [{highWaterMark: -1}, {highWaterMark: NaN}, {size: null}, {size: 3}] as any[]) {
        try {new ReadableStream({}, options);} catch (error) {console.log('strategy', (error as Error).name);}
    }
    for (const source of [3, {type: 'wrong'}, {type: null}, {start: 3}, {pull: 3}, {cancel: 3}] as any[]) {
        try {new ReadableStream(source);} catch (error) {console.log('source', (error as Error).name);}
    }
    for (const highWaterMark of [null, 0, 2, Infinity] as any[]) {
        const stream = new ReadableStream({start(controller) {console.log('mark', controller.desiredSize);controller.close();}}, {highWaterMark});
        await stream.cancel();
    }
    for (const member of ['start', 'pull', 'cancel']) {
        const stream = new ReadableStream({[member]: null} as any);
        await stream.cancel();console.log('null callback', member);
    }
    try {new ReadableStream({start() {throw 'start failure';}});} catch (error) {console.log('start throw', error);}

    const rejectedStart = new ReadableStream({start() {return Promise.reject('start rejected');}});
    try {await rejectedStart.getReader().read();} catch (error) {console.log('start rejection', error);}
    const rejectedPull = new ReadableStream({pull() {return Promise.reject('pull rejected');}});
    const rejectedReader = rejectedPull.getReader();
    try {await rejectedReader.read();} catch (error) {console.log('pull rejection', error);}
    try {await rejectedReader.closed;} catch (error) {console.log('pull closed', error);}

    let controller!: ReadableStreamDefaultController<number>;
    const stream = new ReadableStream<number>({start(value) {controller = value;}}, {highWaterMark: 0});
    console.log('controller', controller instanceof ReadableStreamDefaultController, Object.prototype.toString.call(controller));
    const reader = stream.getReader();
    console.log('closed identity', reader.closed === reader.closed);
    try {stream.getReader();} catch (error) {console.log('locked reader', (error as Error).name);}
    try {await stream.cancel();} catch (error) {console.log('locked cancel', (error as Error).name);}
    const waiting = reader.read();controller.error('broken');
    try {await waiting;} catch (error) {console.log('pending error', error);}
    try {await reader.closed;} catch (error) {console.log('error closed', error);}
    console.log('errored size', controller.desiredSize);
    controller.error('ignored');
    for (const action of [() => controller.enqueue(1), () => controller.close()]) {
        try {action();} catch (error) {console.log('errored action', (error as Error).name);}
    }
    reader.releaseLock();

    let closeController!: ReadableStreamDefaultController;
    const closed = new ReadableStream({start(value) {closeController = value;value.close();}});
    console.log('closed size', closeController.desiredSize, (await closed.getReader().read()).done);
    for (const action of [() => closeController.enqueue(1), () => closeController.close()]) {
        try {action();} catch (error) {console.log('closed action', (error as Error).name);}
    }
    closeController.error('ignored');
    for (const amount of [-1, Infinity, NaN]) {
        let sizeController!: ReadableStreamDefaultController;
        const invalidSize = new ReadableStream({start(value) {sizeController = value;}}, {highWaterMark: 0, size() {return amount;}});
        try {sizeController.enqueue(1);} catch (error) {console.log('size throw', (error as Error).name);}
        try {await invalidSize.getReader().read();} catch (error) {console.log('size read', (error as Error).name);}
    }
    const cancelFailure = new ReadableStream({cancel() {throw 'cancel failed';}});
    try {await cancelFailure.cancel('reason');} catch (error) {console.log('cancel rejection', error);}
    console.log('cancel closes', (await cancelFailure.getReader().read()).done);
    const constructor = ReadableStreamDefaultController as any;
    try {new constructor();} catch (error) {console.log('illegal constructor', (error as Error).name);}
}
main().catch(error => console.log('failure', String(error)));
