export {};
async function main():Promise<void>{
    let reads=0;
    const stream=new ReadableStream({type:'bytes',pull(c){
        const request=c.byobRequest!;
        if(reads===2){c.close();request.respond(0);return;}
        const view=request.view as Uint8Array;view[0]=++reads;request.respond(1);
    }});
    const branches=stream.tee();const byob=branches[0].getReader({mode:'byob'});
    console.log('first',(await byob.read(new Uint8Array(8))).value![0]);
    console.log('second',(await byob.read(new Uint8Array(8))).value![0]);
    console.log('closed',(await byob.read(new Uint8Array(8))).done);
    const other=branches[1].getReader();console.log('other',(await other.read()).value![0],(await other.read()).value![0],(await other.read()).done);
    console.log('source locked',stream.locked,reads);
    let count=0;
    const mixed=new ReadableStream({type:'bytes',pull(c){
        const request=c.byobRequest;
        if(request){(request.view as Uint8Array)[0]=++count;request.respond(1);}else c.enqueue(new Uint8Array([++count]));
        if(count===3)c.close();
    }}).tee();
    let first=mixed[0].getReader();console.log('default',(await first.read()).value![0]);first.releaseLock();
    const switched=mixed[0].getReader({mode:'byob'});console.log('switch byob',(await switched.read(new Uint8Array(4))).value![0]);switched.releaseLock();
    first=mixed[0].getReader();console.log('switch default',(await first.read()).value![0],(await first.read()).done);
    console.log('other response',Array.from(new Uint8Array(await new Response(mixed[1]).arrayBuffer())).join(','));
}
main();
