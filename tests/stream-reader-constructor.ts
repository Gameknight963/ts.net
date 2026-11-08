export {};
async function main() {
    const response = new Response('abc');
    const stream = response.body!;
    const reader = new ReadableStreamDefaultReader(stream);
    console.log('identity', reader instanceof ReadableStreamDefaultReader, reader.constructor === ReadableStreamDefaultReader, Object.getPrototypeOf(reader) === ReadableStreamDefaultReader.prototype, globalThis.ReadableStreamDefaultReader === ReadableStreamDefaultReader);
    console.log('locked', stream.locked, response.bodyUsed);
    try {new ReadableStreamDefaultReader(stream);} catch (error) {console.log('locked constructor', (error as Error).name);}
    const first = await reader.read();
    console.log('read', first.done, Array.from(first.value!).join(','), response.bodyUsed);
    console.log('end', (await reader.read()).done);
    reader.releaseLock();
    console.log('released', stream.locked);
    const automatic = new Response('next').body!.getReader();
    console.log('automatic', automatic.constructor === ReadableStreamDefaultReader, automatic instanceof ReadableStreamDefaultReader);
    await automatic.cancel(); automatic.releaseLock();
    class Derived extends ReadableStreamDefaultReader<Uint8Array> {field = 7; #secret = 8; readSecret() {return this.#secret;}}
    const child = new Derived(new Response('x').body!);
    console.log('derived', child.field, child.readSecret(), child instanceof Derived, child instanceof ReadableStreamDefaultReader, (await child.read()).value![0]);
    child.releaseLock();
    for (const value of [undefined, null, {}, {getReader() {throw new Error('not native');}}]) {
        try {new ReadableStreamDefaultReader(value as any);} catch (error) {console.log('brand', (error as Error).name);}
    }
    try {(ReadableStreamDefaultReader as any)(new Response().body);} catch (error) {console.log('requires new', (error as Error).name);}
    for (const mode of ['valid', 'invalid stream', 'prototype error']) {
        const stream = new Response('z').body!;
        let trace = '';
        function Alternate() {}
        const target = Alternate.bind(null);
        const prototype = {};
        Object.defineProperty(target, 'prototype', {get() {trace += 'prototype|'; if (mode === 'prototype error') throw new Error('prototype'); return prototype;}});
        try {
            const reader = Reflect.construct(ReadableStreamDefaultReader, [mode === 'invalid stream' ? {} : stream], target);
            console.log('new target', mode, Object.getPrototypeOf(reader) === prototype, stream.locked, (await ReadableStreamDefaultReader.prototype.read.call(reader)).value![0], trace);
            ReadableStreamDefaultReader.prototype.releaseLock.call(reader);
        } catch (error) {console.log('new target error', mode, (error as Error).name, stream.locked, trace);}
    }
    console.log('metadata', ReadableStreamDefaultReader.name, ReadableStreamDefaultReader.length, Object.prototype.toString.call(reader));
    for (const [value, key] of [[ReadableStreamDefaultReader, 'prototype'], [ReadableStreamDefaultReader.prototype, 'constructor']] as [any, string][]) {
        const descriptor = Object.getOwnPropertyDescriptor(value, key)!;
        console.log('descriptor', key, descriptor.writable, descriptor.enumerable, descriptor.configurable);
    }
}
main().catch(error => console.log('failure', String(error)));
