export {};
async function drain<T>(stream: ReadableStream<T>): Promise<T[]> {
    const reader = stream.getReader();const values: T[] = [];
    for (;;) {const item = await reader.read();if (item.done) break;values.push(item.value);}
    reader.releaseLock();return values;
}
async function main(): Promise<void> {
    let pulls = 0;
    const source = new ReadableStream<number>({pull(controller) {controller.enqueue(++pulls);if (pulls === 4) controller.close();}});
    const [first,second] = source.tee();
    console.log('branches', source.locked, first instanceof ReadableStream, second instanceof ReadableStream, first !== second);
    try {source.getReader();} catch (error) {console.log('source locked', (error as Error).name);}
    console.log('first', (await drain(first)).join(','));
    console.log('second', (await drain(second)).join(','), pulls, source.locked);

    const shared = {value: 1};
    const objectStream = new ReadableStream<{value: number}>({start(controller) {controller.enqueue(shared);controller.close();}});
    const [objectsA,objectsB] = objectStream.tee();
    const objectA = (await objectsA.getReader().read()).value!;objectA.value = 2;
    const objectB = (await objectsB.getReader().read()).value!;
    console.log('identity', objectA === objectB, objectB === shared, objectB.value);

    let cancellationCalls = 0;
    let captured: any;
    const cancelSource = new ReadableStream({cancel(reason) {cancellationCalls++;captured = reason;}});
    const [cancelA,cancelB] = cancelSource.tee();
    let settled = false;
    const cancelledA = cancelA.cancel('a').then(() => {settled = true;});
    await Promise.resolve();console.log('one cancelled', settled, cancellationCalls);
    await cancelB.cancel('b');await cancelledA;
    console.log('both cancelled', settled, cancellationCalls, captured.join(','));

    const finite = new ReadableStream<number>({start(controller) {controller.enqueue(5);controller.enqueue(6);controller.close();}});
    const [finiteA,finiteB] = finite.tee();
    const cancelFinite = finiteA.cancel('unused');
    console.log('survivor', (await drain(finiteB)).join(','));
    console.log('finite cancellation', await cancelFinite);

    let errorController!: ReadableStreamDefaultController;
    const failing = new ReadableStream({start(controller) {errorController = controller;}});
    const [errorA,errorB] = failing.tee();
    const readerA = errorA.getReader(),readerB = errorB.getReader();
    const failure = {reason: 'source'};
    const errors = [readerA.read().catch(error => error),readerB.read().catch(error => error)];
    errorController.error(failure);
    const caught = await Promise.all(errors);
    console.log('errors', caught[0] === failure, caught[1] === failure);

    const empty = new ReadableStream({start(controller) {controller.close();}});
    const [emptyA,emptyB] = empty.tee();
    console.log('empty', (await emptyA.getReader().read()).done, (await emptyB.getReader().read()).done);

    const nested = new ReadableStream<number>({start(controller) {controller.enqueue(7);controller.close();}});
    const [outerA,outerB] = nested.tee();const [innerA,innerB] = outerA.tee();
    console.log('nested', (await drain(innerA)).join(','), (await drain(innerB)).join(','), (await drain(outerB)).join(','));
}
main().catch(error => console.log('failure', String(error)));
