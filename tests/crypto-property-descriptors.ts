export {};
async function main(): Promise<void> {
    const subtle = crypto.subtle;
    const key = await subtle.importKey('raw', new Uint8Array([1, 2, 3]), {name: 'HMAC', hash: 'SHA-256'}, true, ['sign', 'verify']);
    console.log('algorithm', key.algorithm.name, key.usages.join(','));
    for (const value of [crypto, subtle, key]) {
        Object.defineProperty(value, 'label', {value: 'crypto', enumerable: true});
        Object.freeze(value);
        console.log('frozen', Object.isFrozen(value), Object.keys(value).join(','), Reflect.set(value, 'label', 'other'), Reflect.defineProperty(value, 'extra', {value: 1}));
        let prototype = Object.getPrototypeOf(value);
        while (!Object.prototype.hasOwnProperty.call(prototype, Symbol.toStringTag)) prototype = Object.getPrototypeOf(prototype);
        const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
        console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
    }
    const bytes = new Uint8Array(8);
    console.log('random', crypto.getRandomValues(bytes) === bytes, crypto.randomUUID().length, crypto.subtle === subtle);
    const message = new TextEncoder().encode('hello');
    const signature = await subtle.sign('HMAC', key, message);
    console.log('sign', await subtle.verify('HMAC', key, signature, message), signature.byteLength);
    console.log('key', key.type, key.extractable, key.algorithm.name, key.usages.join(','), new Uint8Array(await subtle.exportKey('raw', key)).join(','));
    console.log('digest', new Uint8Array(await subtle.digest('SHA-256', message)).join(','));
}
main().catch(error => console.log('failure', String(error)));
