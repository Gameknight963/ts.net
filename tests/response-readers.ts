export {};
async function main():Promise<void>{
 const response=new Response("data");const stream=response.body!;console.log("stream",stream===response.body,stream.locked,response.bodyUsed,Object.prototype.toString.call(stream));
 const reader=stream.getReader();console.log("locked",stream.locked,response.bodyUsed,Object.prototype.toString.call(reader));
 try{response.clone();}catch(error){console.log("clone locked",(error as Error).name);}
 try{await response.text();}catch(error){console.log("text locked",(error as Error).name,response.bodyUsed);}
 reader.releaseLock();console.log("released",stream.locked,response.bodyUsed);
 const second=stream.getReader();const first=await second.read();console.log("read",first.done,[...first.value!].join(","),response.bodyUsed,stream.locked);
 await second.closed;console.log("closed",(await second.read()).done);second.releaseLock();console.log("unlocked",stream.locked);
 try{await second.read();}catch(error){console.log("read released",(error as Error).name);}
 try{await second.closed;}catch(error){console.log("closed released",(error as Error).name);}
 const cancelled=new Response("cancel");await cancelled.body!.cancel();console.log("cancel",cancelled.bodyUsed,cancelled.body!.locked,(await cancelled.body!.getReader().read()).done);
 const readerCancel=new Response("cancel");const third=readerCancel.body!.getReader();await third.cancel();console.log("reader cancel",readerCancel.bodyUsed,readerCancel.body!.locked,(await third.read()).done);third.releaseLock();
 const cloneSource=new Response(new Uint8Array([1,2,3]));const copy=cloneSource.clone();const fourth=cloneSource.body!.getReader();const chunk=(await fourth.read()).value!;chunk[0]=9;console.log("clone snapshot",[...new Uint8Array(await copy.arrayBuffer())].join(","));fourth.releaseLock();
 const shared=new Response("shared");const reused=new Response(shared.body);console.log("shared stream",shared.body===reused.body,await reused.text(),shared.bodyUsed);
 const pending=new Response("value").body!.getReader();const pendingClosed=pending.closed;pending.releaseLock();try{await pendingClosed;}catch(error){console.log("pending closed",(error as Error).name);}
 const settled=new Response("value").body!.getReader();const settledClosed=settled.closed;await settled.read();await settledClosed;settled.releaseLock();await settledClosed;console.log("old closed fulfilled");
 const asynchronous=new Response("async");let total=0;for await(const chunk of asynchronous.body!){total+=chunk.byteLength;}console.log("async iteration",total,asynchronous.bodyUsed,asynchronous.body!.locked);
 const untouched=new Response("untouched");const iterator=untouched.body!.values({preventCancel:true});await iterator.return!();console.log("prevent cancel",untouched.bodyUsed,untouched.body!.locked,await untouched.text());
 const early=new Response("early");await early.body!.values().return!();console.log("early return",early.bodyUsed,early.body!.locked);
 const empty=new Response("");const emptyReader=empty.body!.getReader();const zero=await emptyReader.read();console.log("empty reader",zero.done,zero.value?.byteLength,(await emptyReader.read()).done);emptyReader.releaseLock();
}
main().catch(error=>{console.log("unexpected",String(error));throw error;});
