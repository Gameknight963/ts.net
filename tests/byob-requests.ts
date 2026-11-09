export {};
async function main():Promise<void>{
    let count=0;let old:ReadableStreamBYOBRequest|undefined;let saved:Uint8Array|undefined;
    const stream=new ReadableStream({type:'bytes',pull(c){
        const request=c.byobRequest!;const view=request.view as Uint8Array<ArrayBuffer>;
        console.log('request',request instanceof ReadableStreamBYOBRequest,request===c.byobRequest,view.byteOffset,view.byteLength);
        view[0]=++count;old=request;saved=view;request.respond(1);
        console.log('invalidated',request.view===null,saved.byteLength);
        if(count===4)c.close();
    }});
    const reader=stream.getReader({mode:'byob'});
    const first=await reader.read(new Uint16Array(2),{min:2});console.log('respond',Array.from(first.value!).join(','),count,old!.view===null);
    console.log('end',(await reader.read(new Uint8Array(2))).done);
    const replacement=new ReadableStream({type:'bytes',pull(c){const request=c.byobRequest!;const view=request.view as Uint8Array<ArrayBuffer>;view.set([65,66]);request.respondWithNewView(new DataView(view.buffer,view.byteOffset,2));c.close();}}).getReader({mode:'byob'});
    const replaced=await replacement.read(new Uint8Array(8));console.log('new view',Array.from(replaced.value!).join(','));
    const transferred=new ReadableStream({type:'bytes',pull(c){const request=c.byobRequest!;const view=request.view as Uint8Array<ArrayBuffer>;view[0]=42;const offset=view.byteOffset;const buffer=structuredClone(view.buffer,{transfer:[view.buffer]});request.respondWithNewView(new Uint8Array(buffer,offset,1));c.close();}}).getReader({mode:'byob'});
    console.log('transferred view',(await transferred.read(new Uint8Array(4))).value![0]);
    let autoRequest:ReadableStreamBYOBRequest|undefined;
    const auto=new ReadableStream({type:'bytes',autoAllocateChunkSize:8,pull(c){const request=c.byobRequest!;autoRequest=request;const view=request.view as Uint8Array<ArrayBuffer>;console.log('allocated',view.byteLength);view[0]=90;request.respond(1);c.close();}});
    const autoReader=auto.getReader();const value=await autoReader.read();console.log('auto',value.done,Array.from(value.value!).join(','),autoRequest!.view===null);
    console.log('auto end',(await autoReader.read()).done);
    let closedRequest:ReadableStreamBYOBRequest|undefined;
    const empty=new ReadableStream({type:'bytes',pull(c){closedRequest=c.byobRequest!;c.close();closedRequest.respond(0);}}).getReader({mode:'byob'});
    const closed=await empty.read(new Uint8Array(4));console.log('close response',closed.done,closed.value!.byteLength,closedRequest!.view===null);
}
main();
