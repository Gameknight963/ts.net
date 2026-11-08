export {};
async function main(): Promise<void> {
    const values: number[] = [];
    const transform = new TransformStream<number,number>({
        start(controller) {console.log('start', controller.desiredSize, controller instanceof TransformStreamDefaultController);},
        async transform(value,controller) {await Promise.resolve();controller.enqueue(value*2);},
        flush(controller) {controller.enqueue(9);}
    });
    const source = new ReadableStream<number>({start(controller) {controller.enqueue(1);controller.enqueue(2);controller.close();}});
    await source.pipeThrough(transform).pipeTo(new WritableStream<number>({write(value) {values.push(value);}}));
    console.log('pipeline', values.join(','), transform.readable.locked, transform.writable.locked);

    const identity = new TransformStream<number,number>();
    const writer = identity.writable.getWriter(),reader = identity.readable.getReader();
    let written = false;const write = writer.write(3).then(() => {written = true;});
    for(let tick=0;tick<5;tick++)await Promise.resolve();console.log('pressure', written);
    console.log('identity', (await reader.read()).value);await write;
    await writer.close();console.log('closed', (await reader.read()).done, await writer.closed);

    const framed = new TransformStream<string,string>({start(controller) {controller.enqueue('prefix');},transform(value,controller) {controller.enqueue(value);},flush(controller) {controller.enqueue('suffix');}});
    const input = new ReadableStream<string>({start(controller) {controller.enqueue('body');controller.close();}});
    const output: string[] = [];
    await input.pipeThrough(framed).pipeTo(new WritableStream<string>({write(value) {output.push(value);}}));
    console.log('framed', output.join(','));

    let controller!: TransformStreamDefaultController<number>;
    const buffered = new TransformStream<number,number>({start(value) {controller = value;}},undefined,new CountQueuingStrategy({highWaterMark: 2}));
    const bufferedWriter = buffered.writable.getWriter();await bufferedWriter.write(4);await bufferedWriter.write(5);
    console.log('buffered', controller.desiredSize);const bufferedReader = buffered.readable.getReader();
    console.log('buffered values', (await bufferedReader.read()).value, (await bufferedReader.read()).value);
    await bufferedWriter.close();console.log('buffered closed', (await bufferedReader.read()).done);

    class Child extends TransformStream<number,number> {field = 7;}
    const child = new Child();
    console.log('subclass', child instanceof Child, child instanceof TransformStream, child.field, child.readable instanceof ReadableStream, child.writable instanceof WritableStream);
    await child.readable.cancel();
    console.log('metadata', TransformStream.length, TransformStreamDefaultController.length, TransformStreamDefaultController.prototype.enqueue.length, TransformStreamDefaultController.prototype.error.length);
}
main().catch(error => console.log('failure', String(error)));
