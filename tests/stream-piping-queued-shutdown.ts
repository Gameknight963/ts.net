export {};
async function main(): Promise<void> {
    let sourceController!: ReadableStreamDefaultController<number>;
    let pulls = 0,finish!: () => void;
    const values: number[] = [];
    const source = new ReadableStream<number>({start(controller) {sourceController = controller;},pull(controller) {controller.enqueue(++pulls);}}, {highWaterMark: 0});
    const sink = new WritableStream<number>({
        write(value) {values.push(value);return new Promise<void>(resolve => {finish = resolve;});},
        abort(reason) {console.log('abort', reason);}
    }, {highWaterMark: 3});
    const pipe = source.pipeTo(sink).catch(error => error);
    for(let tick=0;tick<30;tick++)await Promise.resolve();
    sourceController.error('source');
    for(let index=0;index<3;index++){finish();for(let tick=0;tick<10;tick++)await Promise.resolve();}
    console.log('drained', await pipe, values.join(','), source.locked, sink.locked);

    let reject!: (reason: unknown) => void;
    const closedSource = new ReadableStream<number>({start(controller) {controller.enqueue(1);controller.close();}});
    const failedSink = new WritableStream({write() {return new Promise<void>((_,fail) => {reject = fail;});}});
    const failedPipe = closedSource.pipeTo(failedSink).catch(error => error);
    for(let tick=0;tick<10;tick++)await Promise.resolve();reject('write');
    console.log('last write error', await failedPipe, closedSource.locked, failedSink.locked);
}
main().catch(error => console.log('failure', String(error)));
