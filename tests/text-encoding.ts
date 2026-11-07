const encoder = new TextEncoder();
const decoder = new TextDecoder();
console.log(encoder.encoding, decoder.encoding, decoder.fatal, decoder.ignoreBOM);
for (const text of ['', 'hello', 'caf\u00e9', '\u4f60\u597d', '\ud83d\ude00', '\ud800x\udc00']) {
    const encoded = encoder.encode(text);
    console.log(encoded instanceof Uint8Array, encoded.join(','), decoder.decode(encoded));
}
console.log(encoder.encode().length, decoder.decode().length);
const backing = new Uint8Array([0, 65, 66, 67, 0]);
console.log(decoder.decode(backing.subarray(1, 4)), decoder.decode(new DataView(backing.buffer, 1, 3)));
console.log(decoder.decode(new Uint8Array([65, 66]).buffer));
for (let size = 0; size <= 8; size++) {
    const output = new Uint8Array(size);
    const result = encoder.encodeInto('A\ud83d\ude00\u00e9Z', output);
    console.log(size, result.read, result.written, output.join(','));
}
const target = new Uint8Array([9, 9, 9, 9]);
const partial = encoder.encodeInto('ab', target.subarray(1, 3));
console.log(partial.read, partial.written, target.join(','));
console.log(decoder.decode(new Uint8Array([0xef, 0xbb, 0xbf, 65])));
console.log(new TextDecoder('utf-8', {ignoreBOM: true}).decode(new Uint8Array([0xef, 0xbb, 0xbf, 65])).charCodeAt(0));
const streamed = new TextDecoder();
console.log(streamed.decode(new Uint8Array([0xf0, 0x9f]), {stream: true}).length);
console.log(streamed.decode(new Uint8Array([0x98]), {stream: true}).length);
console.log(streamed.decode(new Uint8Array([0x80])));
console.log(streamed.decode(new Uint8Array([0xc3]), {stream: true}).length, streamed.decode());
console.log(streamed.decode(new Uint8Array([65])));
console.log(decoder.decode(new Uint8Array([0xff, 65, 0xc3, 0x28])));
const fatal = new TextDecoder('utf8', {fatal: true});
try { fatal.decode(new Uint8Array([0xff])); } catch (error) { console.log(error instanceof TypeError); }
console.log(fatal.decode(new Uint8Array([65])));
console.log(new TextDecoder('UTF-16LE').decode(new Uint8Array([0xff, 0xfe, 65, 0, 0x3d, 0xd8, 0, 0xde])));
console.log(new TextDecoder('utf-16be').decode(new Uint8Array([0xfe, 0xff, 0, 65])));
console.log(new TextDecoder('latin1').encoding, new TextDecoder('ascii').decode(new Uint8Array([0x80, 0x91, 0xe9])));
console.log(new TextDecoder(' \tUtF8\r\n').encoding);
class Encoder extends TextEncoder { roundtrip(text: string) { return decoder.decode(this.encode(text)); } }
console.log(new Encoder().roundtrip('hello'), new Encoder() instanceof TextEncoder);
const E = TextEncoder;
console.log(new E().encode('abc').length);
try { new TextDecoder('invalid'); } catch (error) { console.log(error instanceof RangeError); }
try { (decoder as any).decode([65]); } catch (error) { console.log(error instanceof TypeError); }
try { (encoder as any).encodeInto('a', new Uint16Array(2)); } catch (error) { console.log(error instanceof TypeError); }

const wideStream = new TextDecoder('utf-16le');
console.log(wideStream.decode(new Uint8Array([0xff]), {stream: true}).length);
console.log(wideStream.decode(new Uint8Array([0xfe, 0x3d, 0xd8]), {stream: true}).length);
console.log(wideStream.decode(new Uint8Array([0, 0xde])));
const bomStream = new TextDecoder();
console.log(bomStream.decode(new Uint8Array([0xef]), {stream: true}).length);
console.log(bomStream.decode(new Uint8Array([0xbb]), {stream: true}).length);
console.log(bomStream.decode(new Uint8Array([0xbf, 65])));
console.log(bomStream.decode(new Uint8Array([0xef, 0xbb, 0xbf, 66])));
class Decoder extends TextDecoder { read(bytes: Uint8Array) { return this.decode(bytes); } }
console.log(new Decoder().read(new Uint8Array([65])), new Decoder() instanceof TextDecoder);
console.log(new TextDecoder('iso_8859-1:1987').encoding);
const surrogateOutput = new Uint8Array(3);
console.log(encoder.encodeInto('\ud800', surrogateOutput).written, surrogateOutput.join(','));

const fatalStream = new TextDecoder('utf8', {fatal: true});
console.log(fatalStream.decode(new Uint8Array([0xe2]), {stream: true}).length);
try { fatalStream.decode(new Uint8Array([0x28])); } catch (error) { console.log(error instanceof TypeError); }
console.log(fatalStream.decode(new Uint8Array([66])));

function encodeText(input: string): Uint8Array { return new TextEncoder().encode(input); }
function decodeText(input: Uint8Array): string { return new TextDecoder().decode(input); }
console.log(decodeText(encodeText('typed parameters')));
