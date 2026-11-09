export {};
const origin='http://127.0.0.1:0';
async function main():Promise<void>{
    let cancelled:any;let control!:ReadableStreamDefaultController<Uint8Array>;
    const reason={message:'cancel upload'};
    const aborter=new AbortController();
    const stream=new ReadableStream<Uint8Array>({start(c){control=c;},cancel(value){cancelled=value;}});
    const pending=fetch(origin+'/echo',{method:'POST',body:stream,duplex:'half',signal:aborter.signal} as any);
    control.enqueue(new Uint8Array([65]));
    await new Promise(resolve=>setTimeout(resolve,20));aborter.abort(reason);
    try{await pending;throw new Error('Abort succeeded');}catch(error){if(error!==reason)throw error;}
    if(cancelled!==reason)throw new Error('Upload did not receive abort reason');
    let earlyCancelled:any;
    const early=new AbortController();early.abort(reason);
    const earlyStream=new ReadableStream<Uint8Array>({cancel(value){earlyCancelled=value;}});
    try{await fetch(origin+'/echo',{method:'POST',body:earlyStream,duplex:'half',signal:early.signal} as any);}catch(error){if(error!==reason)throw error;}
    if(earlyCancelled!==reason)throw new Error('Pre-aborted upload was not cancelled');
    const invalid=new ReadableStream({start(c){c.enqueue('invalid');c.close();}});
    let failed=false;
    try{await fetch(origin+'/echo',{method:'POST',body:invalid,duplex:'half'} as any);}catch(error){failed=(error as Error).name==='TypeError';}
    if(!failed)throw new Error('Non-byte upload chunk was accepted');
    console.log('PASS upload cancellation and byte validation');
}
main();
