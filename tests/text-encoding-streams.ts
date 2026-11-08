export {};
async function encode(chunks: any[]): Promise<string> {
    const results: string[] = [];
    const source = new ReadableStream<any>({start(controller) {for(const chunk of chunks)controller.enqueue(chunk);controller.close();}});
    await source.pipeThrough(new TextEncoderStream()).pipeTo(new WritableStream<Uint8Array>({write(value) {results.push(Array.from(value).join(','));}}));
    return results.join('|');
}
async function main(): Promise<void> {
    for (const chunks of [[],[''],['A\ud83d','\ude00B'],['\ud800','','\ud800','\udc00'],['\udc00'],['\ud800'],[undefined,null,123,true]]) {
        console.log('encode', JSON.stringify(chunks), await encode(chunks));
    }
    let coercions = 0;
    console.log('coerce', await encode([{toString() {coercions++;return 'ok';}}]), coercions);
    const large: number[] = [];
    const source = new ReadableStream<string>({start(controller) {controller.enqueue('Z'.repeat(1000));controller.close();}});
    await source.pipeThrough(new TextEncoderStream()).pipeTo(new WritableStream<Uint8Array>({write(value) {large.push(value.length,value[0],value[value.length-1]);}}));
    console.log('large', large.join(','));

    const text = ['A\ud83d','\ude00','\u20ac','\ud800','end'];let joined = '';
    const input = new ReadableStream<string>({start(controller) {for(const chunk of text)controller.enqueue(chunk);controller.close();}});
    await input.pipeThrough(new TextEncoderStream()).pipeThrough(new TextDecoderStream()).pipeTo(new WritableStream<string>({write(value) {joined += value;}}));
    console.log('roundtrip', JSON.stringify(joined));
    const encoder = new TextEncoderStream(),decoder = new TextDecoderStream('utf8',{fatal:true,ignoreBOM:true});
    console.log('metadata', encoder.encoding, decoder.encoding, decoder.fatal, decoder.ignoreBOM, TextEncoderStream.length, TextDecoderStream.length);
    console.log('identity', encoder.readable === encoder.readable, encoder.writable === encoder.writable, encoder.readable instanceof ReadableStream, encoder.writable instanceof WritableStream);
    class Child extends TextEncoderStream {field = 7;}
    const child = new Child();console.log('subclass', child instanceof Child, child instanceof TextEncoderStream, child.field);await child.readable.cancel();
    Object.freeze(encoder);console.log('frozen', Object.isFrozen(encoder), encoder.encoding);await encoder.readable.cancel();await decoder.readable.cancel();
}
main().catch(error => console.log('failure', String(error)));
