export {};
function source(chunks: any[]): ReadableStream<any> {
    return new ReadableStream({start(c) {for(const chunk of chunks)c.enqueue(chunk);c.close();}});
}
async function failure(label: string, action: () => any): Promise<void> {
    try {await action();console.log(label,'unexpected');} catch(error) {console.log(label,(error as Error).name);}
}
async function main(): Promise<void> {
    const stream = source([new Uint8Array([65,226]),new Uint8Array([130,172,66])]);
    const response = new Response(stream);
    console.log('initial',response.body===stream,response.bodyUsed,response.headers.get('content-type'));
    const text = response.text();
    console.log('reading',response.bodyUsed,response.body!.locked);
    console.log('text',await text);
    await failure('twice',()=>response.text());
    const original = new Response(source([new Uint8Array([97,98,99])]));
    const oldBody = original.body;
    const clone = original.clone();
    console.log('clone',original.body!==oldBody,original.body!==clone.body,original.bodyUsed,clone.bodyUsed);
    console.log('cloned',await original.text(),await clone.text());
    const json = new Response(source([new TextEncoder().encode('{"value":42}')]));
    console.log('json',(await json.json()).value);
    console.log('buffer',Array.from(new Uint8Array(await new Response(source([new Uint8Array([1,2]),new Uint8Array([3])])).arrayBuffer())).join(','));
    const blob = await new Response(source([new Uint8Array([65])]),{headers:{'content-type':'text/plain'}}).blob();
    console.log('blob',blob.type,blob.size,await blob.text());
    const encoded = source(['a','\ud83d','\ude00']).pipeThrough(new TextEncoderStream());
    console.log('pipeline',await new Response(encoded).text());
    const locked = source([]); const reader=locked.getReader();
    await failure('locked',()=>new Response(locked));reader.releaseLock();
    const used = source([]); const usedReader=used.getReader();await usedReader.read();usedReader.releaseLock();
    await failure('used',()=>new Response(used));
    await failure('status',()=>new Response(source([]),{status:204}));
    await failure('chunk',()=>new Response(source(['bad'])).text());
    const reason={reason:'exact'};
    const errored = new Response(new ReadableStream({start(c){c.error(reason);}}));
    try {await errored.text();}catch(error){console.log('error identity',error===reason,errored.bodyUsed);}
    const cancelled=new Response(source([]));await cancelled.body!.cancel();
    console.log('cancelled',cancelled.bodyUsed);
}
main();
