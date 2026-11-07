let defaults = 0;
function initial() { defaults++; return 2; }
async function* values(limit = initial()): AsyncGenerator<number, string, number> {
    console.log('start', limit);
    const sent: number = yield Promise.resolve(1);
    await Promise.resolve();
    for (let i = 0; i < limit; i++) yield sent + i;
    return Promise.resolve('done');
}
async function* cleanup(): AsyncGenerator<number, string, unknown> {
    try { yield 1; return 'normal'; }
    finally { await Promise.resolve(); yield 2; console.log('cleanup'); }
}
async function* catching(): AsyncGenerator<number | string, void, unknown> {
    try { yield Promise.reject('yield-failed'); }
    catch (error) { yield 'caught:' + error; }
    try { return Promise.reject('return-failed'); }
    catch (error) { yield 'caught:' + error; }
}
async function* counting() { for (let i = 0; i < 3; i++) yield i + 1; }
async function main() {
    const iterator = values();
    console.log('created', defaults);
    const first = iterator.next(999);
    console.log('requested');
    console.log(JSON.stringify(await first));
    const second = iterator.next(10), third = iterator.next(), fourth = iterator.next();
    console.log(JSON.stringify(await Promise.all([second, third, fourth])));
    console.log(JSON.stringify(await iterator.next()));
    const closed = cleanup();
    console.log(JSON.stringify(await closed.next()));
    console.log(JSON.stringify(await closed.return('stopped')));
    console.log(JSON.stringify(await closed.next()));
    const caught = catching();
    console.log(JSON.stringify(await caught.next()));
    console.log(JSON.stringify(await caught.next()));
    console.log(JSON.stringify(await caught.next()));
    let sum = 0;
    for await (const value of counting()) { sum += value; break; }
    console.log('sum', sum);
    const object = { value: 7, async *items() { yield this.value; await Promise.resolve(); yield this.value + 1; } };
    const items: number[] = [];
    for await (const item of object.items()) items.push(item);
    console.log(items.join(','));
    class Numbers { constructor(public value: number) {} async *items() { yield this.value; yield this.value + 2; } }
    for await (const item of new Numbers(20).items()) console.log('class', item);
    const recursive = async function* named(n: number): AsyncGenerator<number, void, unknown> { yield n; if (n > 0) yield* named(n - 1); };
    const recursiveValues: number[] = [];
    for await (const value of recursive(2)) recursiveValues.push(value);
    console.log(recursiveValues.join(','));
    const unopened = cleanup();
    console.log(JSON.stringify(await unopened.return(Promise.resolve('before'))));
    try { await unopened.throw('after'); } catch (error) { console.log(error); }
}
main().catch(error => console.log('unexpected', error));
