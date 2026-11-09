export {};
async function collect(body:ReadableStream<Uint8Array>,size:number):Promise<string>{
    const reader=body.getReader({mode:'byob'});const bytes:number[]=[];
    for(;;){const result=await reader.read(new Uint8Array(size));if(result.value)bytes.push(...result.value);if(result.done)break;}
    await reader.closed;reader.releaseLock();return bytes.join(',');
}
async function main():Promise<void>{
    for(const response of [new Response('abcde'),new Response(new Uint8Array([1,2,3,4,5])),new Response(new Blob(['hello'])),new Response(new URLSearchParams('a=b'))]){
        console.log('response',await collect(response.body!,2),response.bodyUsed,response.body!.locked);
    }
    const request=new Request('https://example.com/',{method:'POST',body:'request'});
    console.log('request',await collect(request.body!,3),request.bodyUsed);
    console.log('data fetch',await collect((await fetch('data:text/plain,data')).body!,2));
    const url=URL.createObjectURL(new Blob(['blob']));const fetched=await fetch(url);URL.revokeObjectURL(url);
    console.log('blob fetch',await collect(fetched.body!,2));
    const payload=new Uint8Array(257);for(let i=0;i<payload.length;i++)payload[i]=(i*17+5)&255;
    console.log('all bytes',await collect(new Response(payload).body!,17));
    const response=new Response('abcdef');const reader=response.body!.getReader({mode:'byob'});
    const view=new Uint8Array(2);const pending=reader.read(view);console.log('transfer',view.byteLength,response.bodyUsed,response.body!.locked);
    console.log('first',Array.from((await pending).value!).join(','));reader.releaseLock();
    const ordinary=response.body!.getReader();console.log('default remainder',Array.from((await ordinary.read()).value!).join(','),(await ordinary.read()).done);ordinary.releaseLock();
    const original=new Response('clone');const clone=original.clone();
    console.log('clone',await collect(original.body!,2),await clone.text());
    const direct=new Response('direct');const directReader=new ReadableStreamBYOBReader(direct.body!);
    console.log('direct',(await directReader.read(new Uint8Array(8))).value!.length);directReader.releaseLock();
    const data=new Response(new Uint8Array([1,2,3,4]));const dataReader=data.body!.getReader({mode:'byob'});
    const words=await dataReader.read(new Uint16Array(2));console.log('words',Array.from(words.value!).join(','),words.done);
    const end=await dataReader.read(new DataView(new ArrayBuffer(4),1,2));console.log('empty view',end.done,end.value instanceof DataView,end.value!.byteOffset,end.value!.byteLength);
    const dataViewReader=new Response(new Uint8Array([9,8,7])).body!.getReader({mode:'byob'});
    const dataView=(await dataViewReader.read(new DataView(new ArrayBuffer(6),1,3))).value!;
    console.log('data view',dataView.byteOffset,dataView.byteLength,dataView.getUint8(0),dataView.getUint8(2));
    const minimum=new Response('ab').body!.getReader({mode:'byob'});const short=await minimum.read(new Uint8Array(8),{min:4});
    console.log('minimum at eof',short.done,Array.from(short.value!).join(','));
    const empty=new Response('').body!.getReader({mode:'byob'});const zero=await empty.read(new Uint8Array(4));console.log('empty',zero.done,zero.value!.length);
}
main();
