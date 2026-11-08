export {};
async function main(): Promise<void> {
    let finishNext!: (result: IteratorResult<number>) => void;
    let returned = 0;
    const reason = {message: 'stop'};
    const source = {[Symbol.asyncIterator]() {return {
        next() {return new Promise<IteratorResult<number>>(resolve => {finishNext = resolve;});},
        return(value: unknown) {returned++;console.log('reason', value === reason);return Promise.resolve({done: true,value: undefined});}
    };}};
    const stream = (ReadableStream as any).from(source) as ReadableStream<number>;
    const reader = stream.getReader();const read = reader.read();
    await Promise.resolve();
    const cancelled = reader.cancel(reason);
    console.log('cancelled read', (await read).done);
    await cancelled;await reader.closed;
    finishNext({done: false,value: 1});
    await Promise.resolve();await Promise.resolve();
    console.log('late chunk', (await reader.read()).done, returned);
    reader.releaseLock();

    let failPull!: (reason: unknown) => void;
    const pulling = new ReadableStream({pull() {return new Promise<void>((_,reject) => {failPull = reject;});}});
    const pullReader = pulling.getReader();const pullRead = pullReader.read();
    await Promise.resolve();await pullReader.cancel();
    failPull('late error');await Promise.resolve();await Promise.resolve();
    console.log('late rejection', (await pullRead).done, (await pullReader.read()).done);

    let finishStart!: () => void;
    const starting = new ReadableStream({start() {return new Promise<void>(resolve => {finishStart = resolve;});},pull() {throw 'must not pull';}});
    const startReader = starting.getReader();const startRead = startReader.read();
    await startReader.cancel();finishStart();
    console.log('cancelled start', (await startRead).done, await startReader.closed);

    const held = new ReadableStream({}, {highWaterMark: 0});
    const heldReader = held.getReader();
    const pending = [heldReader.read(),heldReader.read()].map(promise => promise.catch(error => error.name));
    heldReader.releaseLock();
    console.log('release all', (await Promise.all(pending)).join(','), held.locked);
}
main().catch(error => console.log('failure', String(error)));
