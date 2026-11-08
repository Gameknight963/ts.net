async function check(): Promise<void> {
    const fromAsync = (Array as any).fromAsync;
    for (const next of [undefined, null, 1, {}]) {
        let order = '';
        const source = {
            [Symbol.asyncIterator]() {
                order += 'iterator;';
                return { get next() { order += 'next;'; return next; } };
            }
        };
        class Result { constructor() { order += 'constructor;'; } }
        try { await fromAsync.call(Result, source); }
        catch (error) { console.log('invalid', (error as Error).name, order); }
        order = '';
        class Failure { constructor() { order += 'constructor;'; throw 'construction failed'; } }
        try { await fromAsync.call(Failure, source); }
        catch (error) { console.log('failure', error, order); }
    }
    let capturedNext: unknown = null;
    let reads = 0;
    const repairable = { [Symbol.asyncIterator]() { return { get next() { reads++; return capturedNext; } }; } };
    class Repair { constructor() { capturedNext = () => Promise.resolve({done:true}); } }
    try { await fromAsync.call(Repair, repairable); }
    catch (error) { console.log('captured', (error as Error).name, reads); }
    let accesses = 0;
    const source = { [Symbol.asyncIterator]() { return { get next() { accesses++; return null; } }; } };
    async function* delegate(): AsyncGenerator<number, void, unknown> { yield* source as any; }
    try { await delegate().next(); }
    catch (error) { console.log('delegate', (error as Error).name, accesses); }
    accesses = 0;
    try { for await (const value of source as any) console.log('unexpected', value); }
    catch (error) { console.log('loop', (error as Error).name, accesses); }
}
check();
