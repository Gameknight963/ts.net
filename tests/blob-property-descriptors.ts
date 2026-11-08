export {};
async function main(): Promise<void> {
    const blob = new Blob(['hello'], {type: 'text/plain'});
    const file = new File(['world'], 'note.txt', {type: 'text/plain', lastModified: 123});
    for (const value of [blob, file]) {
        Object.defineProperty(value, 'label', {value: 'data', enumerable: true});
        Object.freeze(value);
        console.log('frozen', Object.isFrozen(value), Object.keys(value).join(','), Reflect.set(value, 'label', 'other'), Reflect.defineProperty(value, 'extra', {value: 1}));
        console.log('read', value.size, value.type, await value.text(), await value.slice(1, 4).text(), new Uint8Array(await value.arrayBuffer()).join(','));
    }
    console.log('file', file.name, file.lastModified, file instanceof Blob, file instanceof File);
    const shadow = new Blob(['abc']);
    Object.defineProperty(shadow, 'size', {value: 99, configurable: true});
    Object.seal(shadow);
    console.log('shadow', shadow.size, await shadow.text(), await shadow.slice(1).text(), Object.isSealed(shadow), Reflect.deleteProperty(shadow, 'size'));
    for (const prototype of [Blob.prototype, File.prototype]) {
        const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
        console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
    }
}
main().catch(error => console.log('failure', String(error)));
