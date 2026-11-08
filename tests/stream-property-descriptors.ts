export {};
async function main(): Promise<void> {
    const stream = new Blob(['abc']).stream();
    Object.defineProperty(stream, 'label', {value: 'stream', enumerable: true});
    Object.freeze(stream);
    const reader = stream.getReader();
    Object.defineProperty(reader, 'label', {value: 'reader', enumerable: true});
    Object.freeze(reader);
    console.log('frozen', Object.isFrozen(stream), Object.isFrozen(reader), stream.locked, Object.keys(stream).join(','), Object.keys(reader).join(','));
    console.log('writes', Reflect.set(stream, 'label', 'other'), Reflect.defineProperty(reader, 'extra', {value: 1}));
    const first = await reader.read();
    console.log('read', first.done, first.value!.join(','), (await reader.read()).done);
    await reader.closed;
    reader.releaseLock();
    console.log('released', stream.locked);
    const other = new Response('xy').body!;
    const iterator = other.values();
    Object.defineProperty(iterator, 'label', {value: 'iterator', enumerable: true});
    Object.freeze(iterator);
    console.log('iterator', (await iterator.next()).value!.join(','), (await iterator.next()).done, other.locked, Object.isFrozen(iterator));
    const cancelled = Object.freeze(new Response('cancel').body!);
    await cancelled.cancel();
    const after = cancelled.getReader();
    console.log('cancelled', (await after.read()).done);
    after.releaseLock();
    for (const value of [stream, reader]) {
        const tag = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(value), Symbol.toStringTag)!;
        console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
    }
}
main().catch(error => console.log('failure', String(error)));
