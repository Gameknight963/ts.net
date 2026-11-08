export {};
const encoder = new TextEncoder();
Object.defineProperty(encoder, 'label', {value: 'utf8', enumerable: true});
Object.freeze(encoder);
console.log('encoder', encoder.encoding, encoder.encode('abc').join(','), Object.isFrozen(encoder), Object.keys(encoder).join(','), Reflect.set(encoder, 'label', 'other'));
const destination = new Uint8Array(4);
const result = encoder.encodeInto('hello', destination);
console.log('into', result.read, result.written, destination.join(','));
const decoder = new TextDecoder();
Object.defineProperty(decoder, 'label', {get() { return this.encoding; }, enumerable: true});
Object.freeze(decoder);
console.log('decoder', Object.isFrozen(decoder), (decoder as any).label, Reflect.set(decoder, 'extra', 1));
const streaming = Object.seal(new TextDecoder());
console.log('stream', streaming.decode(new Uint8Array([0xe2]), {stream: true}).length, streaming.decode(new Uint8Array([0x82]), {stream: true}).length, streaming.decode(new Uint8Array([0xac])).charCodeAt(0));
console.log('reset', streaming.decode(new Uint8Array([65])));
const closed = Object.preventExtensions(new TextEncoder());
console.log('closed', Object.isExtensible(closed), Reflect.defineProperty(closed, 'extra', {value: 1}), closed.encode('x')[0]);
for (const prototype of [TextEncoder.prototype, TextDecoder.prototype]) {
    const tag = Object.getOwnPropertyDescriptor(prototype, Symbol.toStringTag)!;
    console.log('tag', tag.value, tag.writable, tag.enumerable, tag.configurable);
}
