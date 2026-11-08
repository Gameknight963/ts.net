export {};
async function main(): Promise<void> {
    let finishWrite!: () => void;
    let finishAbort!: () => void;
    const source = new WritableStream<number>({
        write(value) {console.log('inflight', value);return new Promise<void>(resolve => {finishWrite = resolve;});},
        abort(reason) {console.log('abort callback', reason);return new Promise<void>(resolve => {finishAbort = resolve;});}
    });
    const writer = source.getWriter();
    const first = writer.write(1),second = writer.write(2).catch(error => error);
    await Promise.resolve();const aborted = writer.abort('stop');
    console.log('erroring', writer.desiredSize);
    finishWrite();console.log('first finishes', await first);
    console.log('second rejected', await second);
    let closed = false;
    const closing = writer.closed.catch(error => {closed = true;return error;});
    await Promise.resolve();console.log('abort waiting', closed);
    finishAbort();console.log('abort finishes', await aborted, await closing);

    for (const reject of [false,true]) {
        let finish!: () => void;let fail!: (reason: unknown) => void;
        const stream = new WritableStream({close() {return new Promise<void>((resolve,reject) => {finish = resolve;fail = reject;});},abort() {console.log('unexpected abort');}});
        const closeWriter = stream.getWriter();
        const close = closeWriter.close().catch(error => error);
        await Promise.resolve();const abort = closeWriter.abort('stop').catch(error => error);
        if (reject) fail('close failed');else finish();
        console.log('closing abort', reject, await close, await abort);
        console.log('closing result', await closeWriter.closed.then(() => 'closed',error => error));
    }

    let complete!: () => void;
    const retained = new WritableStream({write() {return new Promise<void>(resolve => {complete = resolve;});}});
    const oldWriter = retained.getWriter();const writing = oldWriter.write(3);
    const oldReady = oldWriter.ready.catch(error => error.name),oldClosed = oldWriter.closed.catch(error => error.name);
    oldWriter.releaseLock();
    console.log('release', await oldReady, await oldClosed, retained.locked);
    const newWriter = retained.getWriter();const newClose = newWriter.close();
    await Promise.resolve();complete();
    console.log('retained writes', await writing, await newClose, await newWriter.closed);

    let controller!: WritableStreamDefaultController;
    const frozen = new WritableStream({start(value) {controller = value;},write() {}});
    const frozenWriter = frozen.getWriter();Object.freeze(frozen);Object.freeze(frozenWriter);Object.freeze(controller);
    console.log('frozen', await frozenWriter.write(4), await frozenWriter.close(), Object.isFrozen(frozen));
    frozenWriter.releaseLock();console.log('frozen released', frozen.locked);

    let resolveStart!: () => void;let writes = 0;
    const delayed = new WritableStream({start() {return new Promise<void>(resolve => {resolveStart = resolve;});},write() {writes++;}});
    const delayedWriter = delayed.getWriter();const delayedWrite = delayedWriter.write(5);
    await Promise.resolve();console.log('waiting start', writes);resolveStart();await delayedWrite;
    console.log('started', writes);await delayedWriter.close();

    const zero = new WritableStream({}, {highWaterMark: 0});const zeroWriter = zero.getWriter();
    let zeroReady = false;const zeroWaiting = zeroWriter.ready.then(() => {zeroReady = true;});
    await zeroWriter.write(1);console.log('zero mark', zeroReady, zeroWriter.desiredSize);
    await zeroWriter.close();await zeroWaiting;console.log('zero closed', zeroReady);

    class ChildWriter extends WritableStreamDefaultWriter<number> {field = 8;}
    const childWriter = new ChildWriter(new WritableStream<number>());
    console.log('writer subclass', childWriter instanceof ChildWriter, childWriter instanceof WritableStreamDefaultWriter, childWriter.field);
    await childWriter.close();childWriter.releaseLock();
}
main().catch(error => console.log('failure', String(error)));
