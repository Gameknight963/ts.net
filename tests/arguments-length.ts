function counts(first?: number): void {
    console.log('initial', arguments.length);
    const read = () => arguments.length;
    const saved = arguments;
    saved.length = 8;
    console.log('assigned', read(), arguments.length);
    Object.defineProperty(saved, 'length', {get() { console.log('getter'); return 12; }, configurable: true});
    console.log('accessor', read(), arguments.length);
    delete (saved as any).length;
    console.log('deleted', read(), arguments.length);
    Object.defineProperty(saved, 'length', {value: 3, writable: true, configurable: true});
    console.log('defined', read(), arguments.length);
}
counts(); counts(5);
function defaults(first = arguments.length, second = (() => arguments.length)()): void {
    console.log('defaults', first, second, arguments.length);
}
defaults(); defaults(undefined, 4);
function captures(value: number): () => number {
    const count = () => arguments.length;
    console.log('before escape', count());
    return count;
}
console.log('captured', captures(1)(), (captures as any)(1, 2, 3)());
async function asynchronous(value: number): Promise<void> {
    console.log('async before', arguments.length);
    await Promise.resolve();
    console.log('async after', arguments.length);
}
void asynchronous(1);
function* suspended(value: number): Generator<number> {
    yield arguments.length;
    const saved = arguments;
    yield arguments.length;
    saved.length = 6;
    yield arguments.length;
}
console.log('suspended', [...suspended(1)].join(','));
