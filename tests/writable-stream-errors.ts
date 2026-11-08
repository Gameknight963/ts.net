export {};
async function main(): Promise<void> {
    const startFailure = new WritableStream({start() {return Promise.reject('start');}});
    const startWriter = startFailure.getWriter();
    try {await startWriter.write(1);} catch (error) {console.log('start write', error);}
    try {await startWriter.closed;} catch (error) {console.log('start closed', error);}

    let writes = 0;
    const failing = new WritableStream({write() {writes++;throw 'write';}});
    const failedWriter = failing.getWriter();
    const requests = [failedWriter.write(1),failedWriter.write(2)].map(promise => promise.catch(error => error));
    console.log('write errors', (await Promise.all(requests)).join(','), writes);
    try {await failedWriter.closed;} catch (error) {console.log('write closed', error);}
    console.log('errored abort', await failedWriter.abort('ignored'));

    const closeFailure = new WritableStream({close() {throw 'close';}});
    const closeWriter = closeFailure.getWriter();
    try {await closeWriter.close();} catch (error) {console.log('close error', error);}
    try {await closeWriter.closed;} catch (error) {console.log('close closed', error);}

    const abortFailure = new WritableStream({abort() {return Promise.reject('abort');}});
    const abortWriter = abortFailure.getWriter();
    try {await abortWriter.abort('reason');} catch (error) {console.log('abort error', error);}
    try {await abortWriter.closed;} catch (error) {console.log('abort closed', error);}

    const controllerError = new WritableStream({write(_,controller) {controller.error('controller');}});
    const errorWriter = controllerError.getWriter();
    console.log('inflight succeeds', await errorWriter.write(1));
    try {await errorWriter.write(2);} catch (error) {console.log('controller error', error);}
    try {await errorWriter.closed;} catch (error) {console.log('controller closed', error);}

    for (const amount of [-1,Infinity,NaN]) {
        const invalid = new WritableStream({}, {size() {return amount;}});const writer = invalid.getWriter();
        try {await writer.write(1);} catch (error) {console.log('size', (error as Error).name);}
        console.log('size failed', writer.desiredSize);
    }
    for (const source of [3,{type: 'bytes'},{write: 3},{start: 3},{close: 3},{abort: 3}] as any[]) {
        try {new WritableStream(source);} catch (error) {console.log('sink invalid', (error as Error).name);}
    }
    for (const strategy of [{highWaterMark: -1},{highWaterMark: NaN},{size: null}] as any[]) {
        try {new WritableStream({},strategy);} catch (error) {console.log('strategy invalid', (error as Error).name);}
    }
}
main().catch(error => console.log('failure', String(error)));
