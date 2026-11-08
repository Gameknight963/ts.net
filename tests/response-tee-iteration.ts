/// <reference lib="dom.asynciterable" />
async function run():Promise<void>{
 const [left,right]=new Blob(['iterator']).stream().tee();
 const iterator=left.values();let returned=false;
 const pending=(iterator as any).return('left').then((result:IteratorResult<Uint8Array,unknown>)=>{returned=true;console.log('return value',result.done,result.value);});
 await Promise.resolve();console.log('pending return',returned,left.locked);
 console.log('other',await new Response(right).text());
 await pending;console.log('released',left.locked);
 const [keep,other]=new Blob(['kept']).stream().tee();
 const preserving=keep.values({preventCancel:true});
 await preserving.return!();
 console.log('prevent cancel',keep.locked,await new Response(keep).text(),await new Response(other).text());
 const [a,b]=new Blob([new Uint8Array([1,2,3])]).stream().tee();
 const first=a.getReader(),second=b.getReader();
 const chunk=(await first.read()).value!;chunk[0]=99;
 console.log('independent',Array.from((await second.read()).value!).join(','));
 first.releaseLock();second.releaseLock();
 const [nestedBranch,nestedSibling]=new Blob(['nested cancel']).stream().tee();
 const [nestedLeft,nestedRight]=nestedBranch.tee();
 let nestedCanceled=false;
 const nestedCancellation=Promise.all([nestedLeft.cancel(),nestedRight.cancel()]).then(()=>{nestedCanceled=true;});
 await Promise.resolve();await Promise.resolve();console.log('nested pending',nestedCanceled);
 console.log('nested sibling',await new Response(nestedSibling).text());
 await nestedCancellation;console.log('nested canceled',nestedCanceled);
 const root=new Blob(['nested']).stream();
 const [branch,sibling]=root.tee();const [inner1,inner2]=branch.tee();
 console.log('nested',await new Response(inner1).text(),await new Response(inner2).text(),await new Response(sibling).text(),root.locked,branch.locked);
 const consumed=new Blob(['done']).stream();const reader=consumed.getReader();await reader.read();await reader.read();reader.releaseLock();
 const [empty1,empty2]=consumed.tee();
 console.log('consumed',(await empty1.getReader().read()).done,(await empty2.getReader().read()).done);
 const canceled=new Blob(['no']).stream();await canceled.cancel();const [c,d]=canceled.tee();
 console.log('canceled',(await c.getReader().read()).done,(await d.getReader().read()).done);
}
run();
