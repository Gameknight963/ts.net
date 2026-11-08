export {};
async function main(): Promise<void> {
    const request = new Request('https://example.com/', {method: 'POST', body: 'request'});
    const response = new Response('response', {status: 201});
    for (const value of [request, response]) {
        Object.defineProperty(value, 'label', {value: 'http', enumerable: true});
        Object.freeze(value);
        const clone = value.clone();
        console.log('frozen', Object.isFrozen(value), Object.keys(value).join(','), Reflect.set(value, 'label', 'changed'), Reflect.defineProperty(value, 'extra', {value: 1}));
        value.headers.set('x-extra', 'ok');
        console.log('headers', value.headers.get('x-extra'), clone.headers.get('x-extra'));
        console.log('before', value.bodyUsed, clone.bodyUsed);
        console.log('text', await value.text(), await clone.text());
        console.log('after', value.bodyUsed, clone.bodyUsed);
    }
    const shadow = new Response('actual', {status: 202});
    Object.defineProperty(shadow, 'status', {value: 999, configurable: true});
    Object.seal(shadow);
    console.log('shadow', shadow.status, shadow.clone().status, await shadow.text(), Object.isSealed(shadow), Reflect.deleteProperty(shadow, 'status'));
    for (const prototype of [Request.prototype, Response.prototype]) {
        const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
        console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
    }
}
main().catch(error => console.log('failure', String(error)));
