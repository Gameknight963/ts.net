export {};
async function main(): Promise<void> {
    let resolveStart!: () => void;
    let pulls = 0;
    const delayed = new ReadableStream<number>({
        start() {return new Promise<void>(resolve => {resolveStart = resolve;});},
        pull(controller) {pulls++;controller.enqueue(1);controller.close();}
    });
    const delayedReader = delayed.getReader();
    const waiting = delayedReader.read();
    await Promise.resolve();console.log('before start', pulls);
    resolveStart();console.log('after start', (await waiting).value, pulls);
    delayedReader.releaseLock();

    let sizeCalls = 0;
    let control!: ReadableStreamDefaultController<number>;
    const direct = new ReadableStream<number>({start(controller) {control = controller;}}, {
        highWaterMark: 0,size() {sizeCalls++;return 1;}
    });
    const directReader = direct.getReader();
    const first = directReader.read();control.enqueue(2);
    console.log('direct', (await first).value, sizeCalls);
    const second = directReader.read();const closed = directReader.closed;
    await directReader.cancel('done');
    console.log('pending cancel', (await second).done, await closed, control.desiredSize);
    directReader.releaseLock();

    let resolveCancel!: () => void;
    const cancelled = new ReadableStream({cancel(reason) {
        console.log('return reason', reason);
        return new Promise<void>(resolve => {resolveCancel = resolve;});
    }}, {highWaterMark: 0});
    const iterator = (cancelled as any).values();
    const returning = iterator.return('stop');
    await Promise.resolve();console.log('cancel pending', cancelled.locked);
    resolveCancel();const returned = await returning;
    console.log('return resolved', returned.value, returned.done, cancelled.locked);

    const serial = new ReadableStream<number>({start(controller) {controller.enqueue(3);controller.enqueue(4);controller.close();}});
    const serialIterator = (serial as any).values();
    console.log('concurrent iteration', JSON.stringify(await Promise.all([serialIterator.next(),serialIterator.next(),serialIterator.next()])));
    console.log('iteration released', serial.locked);

    let frozenController!: ReadableStreamDefaultController<number>;
    const frozen = new ReadableStream<number>({start(controller) {frozenController = controller;}}, {highWaterMark: 0});
    Object.freeze(frozen);Object.freeze(frozenController);
    const frozenReader = frozen.getReader();Object.freeze(frozenReader);
    const read = frozenReader.read();frozenController.enqueue(5);frozenController.close();
    console.log('frozen', (await read).value, (await frozenReader.read()).done);
    frozenReader.releaseLock();console.log('frozen released', frozen.locked);

    const streamPrototype = ReadableStream.prototype;
    console.log('metadata', ReadableStream.name, ReadableStream.length, streamPrototype.constructor === ReadableStream);
    console.log('buffered prototype', new Blob().stream() instanceof ReadableStream, new Response('x').body!.constructor === ReadableStream);
    for (const name of ['enqueue','close','error']) {
        const descriptor = Object.getOwnPropertyDescriptor(ReadableStreamDefaultController.prototype,name)!;
        console.log('controller method', name, descriptor.value.length, descriptor.writable, descriptor.enumerable, descriptor.configurable);
    }
    const closedGetter = Object.getOwnPropertyDescriptor(ReadableStreamDefaultReader.prototype,'closed')!.get!;
    try {await closedGetter.call({});} catch (error) {console.log('closed brand', (error as Error).name);}
}
main().catch(error => console.log('failure', String(error)));
