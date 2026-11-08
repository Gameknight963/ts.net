export {};
async function main(): Promise<void> {
    const stream = new ReadableStream<number>({start(controller) {
        console.log('start', controller.desiredSize);
        controller.enqueue(1);controller.enqueue(2);
        console.log('queued', controller.desiredSize);
        controller.close();
    }});
    console.log('stream', stream instanceof ReadableStream, stream.locked, Object.prototype.toString.call(stream));
    const reader = new ReadableStreamDefaultReader(stream);
    console.log('reader', stream.locked, reader instanceof ReadableStreamDefaultReader);
    console.log('first', JSON.stringify(await reader.read()));
    console.log('second', JSON.stringify(await reader.read()));
    console.log('end', JSON.stringify(await reader.read()));
    await reader.closed;reader.releaseLock();console.log('released', stream.locked);

    let count = 0, active = 0;
    const pulled = new ReadableStream<number>({async pull(controller) {
        active++;console.log('pull', ++count, active);
        await Promise.resolve();
        controller.enqueue(count);active--;
        if (count === 3) controller.close();
    }}, {highWaterMark: 0});
    const pending = pulled.getReader();
    const reads = [pending.read(), pending.read(), pending.read(), pending.read()];
    console.log('pending', JSON.stringify(await Promise.all(reads)));
    pending.releaseLock();

    let control!: ReadableStreamDefaultController<string>;
    const held = new ReadableStream<string>({start(controller) {control = controller;}}, {highWaterMark: 0});
    const heldReader = held.getReader();
    const waiting = heldReader.read();
    control.enqueue('ready');control.close();
    console.log('external', JSON.stringify(await waiting), (await heldReader.read()).done);
    heldReader.releaseLock();

    let cancelCalls = 0;
    const cancelled = new ReadableStream<number>({
        start(controller) {controller.enqueue(4);},
        async cancel(reason) {cancelCalls++;console.log('cancel', reason);await Promise.resolve();}
    });
    console.log('cancel result', await cancelled.cancel('stop'), cancelCalls, (await cancelled.getReader().read()).done);

    const failure = {message: 'original'};
    const failed = new ReadableStream({start(controller) {controller.error(failure);}});
    const failedReader = failed.getReader();
    try {await failedReader.read();} catch (error) {console.log('read error', error === failure);}
    try {await failedReader.closed;} catch (error) {console.log('closed error', error === failure);}
    try {await failedReader.cancel();} catch (error) {console.log('cancel error', error === failure);}
    failedReader.releaseLock();

    const releaseStream = new ReadableStream({}, {highWaterMark: 0});
    const releaseReader = releaseStream.getReader();
    const oldClosed = releaseReader.closed.catch(error => console.log('old closed', error.name));
    const oldRead = releaseReader.read().catch(error => console.log('old read', error.name));
    releaseReader.releaseLock();await Promise.all([oldClosed,oldRead]);
    try {await releaseReader.read();} catch (error) {console.log('released read', (error as Error).name);}
    console.log('new reader', releaseStream.getReader() instanceof ReadableStreamDefaultReader);

    let sizes = 0;
    const sized = new ReadableStream<string>({start(controller) {
        controller.enqueue('abc');console.log('size', controller.desiredSize);controller.close();
    }}, {highWaterMark: 5, size(chunk) {sizes++;return chunk.length;}});
    console.log('sized', (await sized.getReader().read()).value, sizes);

    let iteration = 0;
    const iterated = new ReadableStream<number>({pull(controller) {
        controller.enqueue(++iteration);if (iteration === 3) controller.close();
    }, cancel(reason) {console.log('iteration cancel', reason);}});
    let sum = 0;
    for await (const value of iterated as any) {sum += value;if (value === 2) break;}
    console.log('iteration', sum, iterated.locked);

    const preserved = new ReadableStream<number>({start(controller) {controller.enqueue(8);controller.enqueue(9);controller.close();}});
    const iterator = (preserved as any).values({preventCancel: true});
    console.log('iterator', (await iterator.next()).value, (await iterator.return('done')).value, preserved.locked);
    console.log('remaining', (await preserved.getReader().read()).value);

    class Child extends ReadableStream<number> {}
    const child = new Child({start(controller) {controller.close();}});
    console.log('subclass', child instanceof Child, child instanceof ReadableStream, (await child.getReader().read()).done);
}
main().catch(error => console.log('failure', String(error)));
