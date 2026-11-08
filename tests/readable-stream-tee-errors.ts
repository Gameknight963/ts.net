export {};
async function main(): Promise<void> {
    let finishCancel!: () => void;
    const cancelled = new ReadableStream({cancel(reason) {
        console.log('reasons', reason.length, reason[0], reason[1]);
        return new Promise<void>(resolve => {finishCancel = resolve;});
    }});
    const [first,second] = cancelled.tee();
    let firstDone = false,secondDone = false;
    const pendingA = first.cancel('first').then(() => {firstDone = true;});
    const pendingB = second.cancel('second').then(() => {secondDone = true;});
    await Promise.resolve();console.log('cancel pending', firstDone, secondDone);
    finishCancel();await Promise.all([pendingA,pendingB]);
    console.log('cancel completed', firstDone, secondDone, cancelled.locked);

    const failure = {reason: 'cancel'};
    const rejected = new ReadableStream({cancel() {return Promise.reject(failure);}});
    const [rejectedA,rejectedB] = rejected.tee();
    const errors = await Promise.all([
        rejectedA.cancel().catch(error => error),
        rejectedB.cancel().catch(error => error)
    ]);
    console.log('cancel failures', errors[0] === failure, errors[1] === failure);

    let controller!: ReadableStreamDefaultController<number>;
    const errored = new ReadableStream<number>({start(value) {controller = value;value.enqueue(1);}});
    const [errorA,errorB] = errored.tee();
    const readerA = errorA.getReader(),readerB = errorB.getReader();
    console.log('first chunk', (await readerA.read()).value);
    controller.error('after chunk');
    try {await readerB.read();} catch (error) {console.log('queued error', error);}
    try {await readerA.read();} catch (error) {console.log('next error', error);}

    let stoppedController!: ReadableStreamDefaultController;
    const stopped = new ReadableStream({start(value) {stoppedController = value;}});
    const [stoppedA,stoppedB] = stopped.tee();
    const cancellation = stoppedA.cancel('unused');
    stoppedController.error('stopped');
    console.log('cancel after source error', await cancellation);
    try {await stoppedB.getReader().read();} catch (error) {console.log('survivor error', error);}

    const rejectedStart = new ReadableStream({start() {return Promise.reject('start');}});
    const [startA,startB] = rejectedStart.tee();
    console.log('start errors', (await Promise.all([
        startA.getReader().read().catch(error => error),
        startB.getReader().read().catch(error => error)
    ])).join(','));

    const locked = new ReadableStream();locked.getReader();
    try {locked.tee();} catch (error) {console.log('locked', (error as Error).name);}
    try {ReadableStream.prototype.tee.call({} as ReadableStream);} catch (error) {console.log('brand', (error as Error).name);}
}
main().catch(error => console.log('failure', String(error)));
