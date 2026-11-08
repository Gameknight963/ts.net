export {};
const from = (ReadableStream as any).from as (value: unknown) => ReadableStream<any>;
async function drain(stream: ReadableStream<any>): Promise<any[]> {
    const reader = stream.getReader(), values: any[] = [];
    for (;;) {const item = await reader.read();if (item.done) break;values.push(item.value);}
    reader.releaseLock();return values;
}
async function main(): Promise<void> {
    console.log('metadata', from.name, from.length);
    console.log('array', (await drain(from([1,2,3]))).join(','));
    console.log('string', (await drain(from('A\u{1f600}B'))).join('|'));
    console.log('set', (await drain(from(new Set([4,5])))).join(','));
    console.log('sync promise', (await drain(from([Promise.resolve(6)]))).join(','));
    function* generated(): Generator<number> {yield 7;yield 8;}
    console.log('generator', (await drain(from(generated()))).join(','));
    async function* asynchronous(): AsyncGenerator<number> {yield 9;await Promise.resolve();yield 10;}
    console.log('async generator', (await drain(from(asynchronous()))).join(','));

    let nextCalls = 0, closed = 0;
    const iterable = {[Symbol.iterator]() {return {
        next() {nextCalls++;return {done: false,value: nextCalls};},
        return(reason: unknown) {closed++;console.log('return reason', reason);return {done: true,value: undefined};}
    };}};
    const stream = from(iterable);
    await Promise.resolve();console.log('lazy', nextCalls);
    const reader = stream.getReader();console.log('read', (await reader.read()).value, nextCalls);
    console.log('cancel', await reader.cancel('stop'), closed);reader.releaseLock();

    let count = 0;
    const promisedValue = Promise.resolve(11);
    const asyncValues = {[Symbol.asyncIterator]() {return {
        next() {return Promise.resolve(count++ ? {done: true,value: undefined} : {done: false,value: promisedValue});}
    };}};
    const asyncReader = from(asyncValues).getReader();
    const result = await asyncReader.read();
    console.log('async value', result.value === promisedValue, await result.value, (await asyncReader.read()).done);

    const both = {
        [Symbol.asyncIterator]() {console.log('async selected');return {next() {return Promise.resolve({done: true});}};},
        [Symbol.iterator]() {throw new Error('sync should not run');}
    };
    console.log('priority', (await from(both).getReader().read()).done);

    let gets = 0;
    const cached = {[Symbol.iterator]() {return {
        get next() {gets++;let current = 0;return () => ({done: current++ > 0,value: 12});}
    };}};
    console.log('cached next', (await drain(from(cached))).join(','), gets <= 2);
    const [first,second] = from([13,14]).tee();
    console.log('tee', (await drain(first)).join(','), (await drain(second)).join(','));
}
main().catch(error => console.log('failure', String(error)));
