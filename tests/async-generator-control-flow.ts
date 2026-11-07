/// <reference lib="esnext.disposable" />
async function* inner(): AsyncGenerator<number, string, unknown> { yield Promise.resolve(7); await Promise.resolve(); yield 8; return 'inner'; }
async function* delegated(): AsyncGenerator<number | string, string, unknown> {
    yield* [Promise.resolve(1), 2];
    const result = yield* inner();
    yield result;
    return 'outer';
}
async function* mixed(): AsyncGenerator<number, string, unknown> {
    try { yield 1; }
    catch (error) { await Promise.resolve(); console.log('caught', error); yield 2; }
    finally { await Promise.resolve(); console.log('closed'); }
    return 'normal';
}
const custom = {
    step: 0,
    [Symbol.asyncIterator]() { return this; },
    async next(value?: unknown) { console.log('delegate-next', value); return {done: this.step++ > 0, value: 10}; },
    async return(value?: unknown) { console.log('delegate-return', value); return {done: false, value: 20}; },
    async throw(value?: unknown) { console.log('delegate-throw', value); return {done: true, value: 30}; }
};
async function* forwarding(): AsyncGenerator<number, number, unknown> { const result = yield* custom; yield Number(result); return 40; }
async function* rejectedReturn(): AsyncGenerator<number | string, string, unknown> {
    try { yield 1; } catch (error) { yield 'return-caught:' + error; }
    return 'done';
}
async function* scopes(): AsyncGenerator<() => number, void, unknown> {
    const read = () => later;
    yield read;
    await Promise.resolve();
    let later = 7;
    yield read;
}
async function* empty() { return 5; }
async function* nested(): AsyncGenerator<number, string, unknown> {
    try { try { yield 1; } finally { await Promise.resolve(); yield 2; } }
    finally { await Promise.resolve(); yield 3; }
    return 'normal';
}
async function* loops() {
    outer: for (let i = 0; i < 3; i++) {
        for (let j = 0; j < 2; j++) {
            try { yield i * 10 + j; if (i === 1) break outer; continue outer; }
            finally { await Promise.resolve(); console.log('loop-finally', i, j); }
        }
    }
    yield 99;
}
async function* defining(): AsyncGenerator<string, number, string> {
 class Defined { [yield await Promise.resolve('key')]() { return 123; } }
 return (new Defined() as any).method();
}
async function* owned() {
 await using resource = {async [Symbol.asyncDispose]() { console.log('dispose-start'); await Promise.resolve(); console.log('dispose-end'); }};
 yield 1; yield 2;
}
async function main() {
    const owner = owned();
    console.log(JSON.stringify(await owner.next()));
    console.log(JSON.stringify(await owner.return()));
    const definition = defining();
    console.log(JSON.stringify(await definition.next()));
    console.log(JSON.stringify(await definition.next('method')));
    const clean = nested();
    console.log(JSON.stringify(await clean.next()));
    console.log(JSON.stringify(await clean.return('early')));
    console.log(JSON.stringify(await clean.next()));
    console.log(JSON.stringify(await clean.next()));
    for await (const item of loops()) console.log('loop', item);
    const source = delegated();
    const all = await Promise.all([source.next(), source.next(), source.next(), source.next(), source.next(), source.next()]);
    console.log(JSON.stringify(all));
    const queue = mixed();
    console.log(JSON.stringify(await Promise.all([queue.next(), queue.throw('reason'), queue.return('early'), queue.next()])));
    const forward = forwarding();
    console.log(JSON.stringify(await forward.next()));
    console.log(JSON.stringify(await forward.return(50)));
    console.log(JSON.stringify(await forward.throw('recover')));
    console.log(JSON.stringify(await forward.next()));
    const rejected = rejectedReturn();
    console.log(JSON.stringify(await rejected.next()));
    console.log(JSON.stringify(await rejected.return(Promise.reject('rejected'))));
    console.log(JSON.stringify(await rejected.next()));
    const captured = scopes();
    const read = (await captured.next()).value as () => number;
    try { read(); } catch (error) { console.log(error instanceof ReferenceError); }
    await captured.next(); console.log(read());
    const many = empty();
    const requests = Array.from({length: 1000}, () => many.next());
    const results = await Promise.all(requests);
    console.log(results.length, results[0].value, results[999].done);
}
main().catch(error => console.log('unexpected', error));
