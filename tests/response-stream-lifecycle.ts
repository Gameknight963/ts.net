export {};
async function main(): Promise<void> {
    let controller!: ReadableStreamDefaultController<Uint8Array>;
    const response=new Response(new ReadableStream<Uint8Array>({start(c){controller=c;}}));
    let complete=false;
    const pending=response.text().then(value=>{complete=true;return value;});
    controller.enqueue(new Uint8Array([65]));
    await Promise.resolve();await Promise.resolve();
    console.log('pending',complete,response.bodyUsed,response.body!.locked);
    controller.enqueue(new Uint8Array([66]));controller.close();
    console.log('complete',await pending,response.body!.locked);
    let failedController!: ReadableStreamDefaultController<Uint8Array>;
    const failed=new Response(new ReadableStream<Uint8Array>({start(c){failedController=c;}}));
    const reason={message:'failure'};
    const failedRead=failed.arrayBuffer().catch(error=>error);
    failedController.enqueue(new Uint8Array([1]));failedController.error(reason);
    console.log('failed',await failedRead===reason,failed.bodyUsed,failed.body!.locked);
    const cancelled:any[]=[];
    const original=new Response(new ReadableStream<Uint8Array>({cancel(reason){cancelled.push(reason);}}));
    const clone=original.clone();
    const first=original.body!.cancel('first');
    const second=clone.body!.cancel('second');
    await Promise.all([first,second]);
    console.log('cancel branches',JSON.stringify(cancelled),original.bodyUsed,clone.bodyUsed);
    const direct=new Response(new ReadableStream<Uint8Array>());
    const reader=direct.body!.getReader();
    console.log('locked unused',direct.bodyUsed);
    const read=reader.read();console.log('read used',direct.bodyUsed);
    await reader.cancel();await read;reader.releaseLock();
    const pipeResponse=new Response(new ReadableStream<Uint8Array>({start(c){c.close();}}));
    const pipe=pipeResponse.body!.pipeTo(new WritableStream());
    console.log('pipe used',pipeResponse.bodyUsed);await pipe;
    const compressed=new Response(new ReadableStream<BufferSource>({start(c){c.enqueue(new TextEncoder().encode('hello'));c.close();}}).pipeThrough(new CompressionStream('gzip')));
    console.log('compressed',await new Response((compressed.body! as ReadableStream<BufferSource>).pipeThrough(new DecompressionStream('gzip'))).text());
    const form=new Response(new ReadableStream<Uint8Array>({start(c){c.enqueue(new TextEncoder().encode('a=hello+world&b=2'));c.close();}}),{headers:{'content-type':'application/x-www-form-urlencoded'}});
    const data=await form.formData();console.log('form',data.get('a'),data.get('b'));
}
main();

