export {};
async function main():Promise<void>{
    const source=new ReadableStream({type:'bytes'});const reader=source.getReader({mode:'byob'});
    const options={get min(){reader.releaseLock();return 1;}};
    const input=new Uint8Array(4);
    try{await reader.read(input,options);}catch(error){console.log('released during options',(error as Error).name,input.byteLength,source.locked);}
    let controller!:ReadableByteStreamController;
    const active=new ReadableStream({type:'bytes',start(c){controller=c;}});const r=active.getReader({mode:'byob'});
    const pending=r.read(new Uint8Array(4));const request=controller.byobRequest!;(request.view as Uint8Array)[0]=42;
    try{request.respond({valueOf(){request.respond(1);return 1;}} as any);}catch(error){console.log('respond reentry',(error as Error).name);}
    console.log('reentry value',(await pending).value![0]);controller.close();
    const detached=new Uint8Array(4);const detaching=new ReadableStream({type:'bytes'}).getReader({mode:'byob'});
    // Node reports RangeError here; validate rejection without depending on that distinction.
    try{await detaching.read(detached,{get min(){structuredClone(detached.buffer,{transfer:[detached.buffer]});return 1;}});}catch(error){const name=(error as Error).name;console.log('detached during options',name==='TypeError'||name==='RangeError');}
    let queued!:ReadableByteStreamController;
    const closed=new ReadableStream({type:'bytes',start(c){queued=c;c.enqueue(new Uint8Array([1,2]));c.close();}}).getReader({mode:'byob'});
    try{await closed.read(new Uint8Array(8),{min:4});}catch(error){console.log('closed minimum',(error as Error).name);}
    const odd=new ReadableStream({type:'bytes',start(c){c.enqueue(new Uint8Array([1]));c.close();}}).getReader({mode:'byob'});
    try{await odd.read(new Uint16Array(2));}catch(error){console.log('closed alignment',(error as Error).name);}
}
main();
