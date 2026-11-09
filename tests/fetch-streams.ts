export {};
const origin='http://127.0.0.1:0';
function body(text:string):ReadableStream<Uint8Array> {return new ReadableStream({start(c){c.enqueue(new TextEncoder().encode(text));c.close();}});}
async function upload(path:string,value:ReadableStream<any>,extra:any={}):Promise<Response> {return fetch(origin+path,{method:'POST',body:value,duplex:'half',...extra} as any);}
async function main():Promise<void>{
    const first=await upload('/echo',body('streamed'),{headers:{'x-test':'stream'}});
    const echo=await first.json();console.log('stream',echo.method,echo.body,echo.header,echo.contentType);
    const original=new Request(origin+'/echo',{method:'POST',body:body('request'),duplex:'half'} as any);
    const clone=original.clone();const result=await fetch(original);
    console.log('request',(await result.json()).body,original.bodyUsed,await clone.text());
    const converted=await upload('/redirect303',body('redirected'));
    const redirected=await converted.json();console.log('303',converted.redirected,redirected.method,redirected.body);
    try{await upload('/redirect307',body('not replayable'));}catch(error){console.log('307',(error as Error).name);}
    const reason={message:'producer failed'};
    try{await upload('/echo',new ReadableStream({start(c){c.error(reason);}}));}catch(error){console.log('producer error',(error as Error).name);}
    let controller!:ReadableStreamDefaultController<Uint8Array>;
    const aborter=new AbortController();
    const pending=upload('/echo',new ReadableStream<Uint8Array>({start(c){controller=c;}}),{signal:aborter.signal});
    controller.enqueue(new Uint8Array([65]));
    await new Promise(resolve=>setTimeout(resolve,20));aborter.abort(reason);
    try{await pending;}catch(error){console.log('abort',error===reason);}
    console.log('empty',(await (await upload('/echo',body(''))).json()).body==='');
    await fetch(origin+'/upload-reset');
    let live!:ReadableStreamDefaultController<Uint8Array>;
    const progressing=upload('/upload-progress',new ReadableStream<Uint8Array>({start(c){live=c;}}));
    live.enqueue(new TextEncoder().encode('first'));
    const deadline=Date.now()+10000;let observed=false;
    while(Date.now()<deadline){
        if(await (await fetch(origin+'/upload-observed')).text()==='true'){observed=true;break;}
        await new Promise(resolve=>setTimeout(resolve,10));
    }
    if(!observed)throw new Error('Upload was buffered until close');
    live.enqueue(new TextEncoder().encode('second'));live.close();
    console.log('incremental',observed,await (await progressing).text());
}
main();
