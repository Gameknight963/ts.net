export {};
async function main(): Promise<void> {
    const source = new ReadableStream<number>({start(controller) {controller.enqueue(1);controller.enqueue(2);controller.enqueue(3);controller.close();}});
    const values: number[] = [];
    const destination = new WritableStream<number>({async write(value) {await Promise.resolve();values.push(value);},close() {console.log('close');}});
    const piping = source.pipeTo(destination);
    console.log('locked', source.locked, destination.locked);
    console.log('done', await piping, values.join(','), source.locked, destination.locked);

    let byteSum = 0;
    const response = new Response('ABC');
    const buffered = new WritableStream<Uint8Array>({write(bytes) {for (const value of bytes) byteSum += value;}});
    const bytes = response.body!.pipeTo(buffered);
    console.log('body used', response.bodyUsed);await bytes;
    console.log('buffered', byteSum, response.body!.locked, buffered.locked);

    let closed = 0;
    const preserved = new WritableStream<number>({write(value) {values.push(value);},close() {closed++;}});
    await new ReadableStream<number>({start(controller) {controller.enqueue(4);controller.close();}}).pipeTo(preserved,{preventClose: true});
    console.log('prevent close', closed, preserved.locked);
    const writer = preserved.getWriter();await writer.write(5);await writer.close();writer.releaseLock();
    console.log('reuse', values.join(','), closed);

    let controller!: ReadableStreamDefaultController<number>;
    const readable = new ReadableStream<number>({start(value) {controller = value;}});
    const writable = new WritableStream<number>({write(value) {controller.enqueue(value*2);},close() {controller.close();}});
    const input = new ReadableStream<number>({start(value) {value.enqueue(6);value.enqueue(7);value.close();}});
    const output = input.pipeThrough({readable,writable});
    const results: number[] = [];
    for await (const value of output as any) results.push(value);
    console.log('through', output === readable, results.join(','), input.locked, writable.locked);

    let pulls = 0,finish!: () => void;
    const queued = new ReadableStream<number>({pull(value) {value.enqueue(++pulls);}}, {highWaterMark: 0});
    const signal = new AbortController();
    const pending = new WritableStream<number>({write() {return new Promise<void>(resolve => {finish = resolve;});}}, {highWaterMark: 3});
    const running = queued.pipeTo(pending,{signal: signal.signal}).catch(error => error);
    for (let index = 0;index < 30;index++) await Promise.resolve();
    console.log('capacity', pulls);
    signal.abort('stop');
    // Let every write already queued before shutdown finish.
    for (let index = 0;index < 3;index++) {finish();for(let tick = 0;tick < 10;tick++)await Promise.resolve();}
    console.log('aborted', await running, queued.locked, pending.locked);

    const empty = new ReadableStream({start(value) {value.close();}});
    const zero = new WritableStream({}, {highWaterMark: 0});
    console.log('empty zero', await empty.pipeTo(zero), empty.locked, zero.locked);
}
main().catch(error => console.log('failure', String(error)));
