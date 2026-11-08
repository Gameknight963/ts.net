export {};
async function run(): Promise<void> {
 const buffer=new SharedArrayBuffer(8,{maxByteLength:32});
 const tracking=new Uint8Array(buffer), fixed=new Uint8Array(buffer,0,4), offset=new Uint16Array(buffer,4);
 const view=new DataView(buffer), fixedView=new DataView(buffer,2,2);
 tracking[0]=7;tracking[7]=9;
 const alias=structuredClone(buffer), aliasView=new Uint8Array(alias);
 console.log('initial',buffer.byteLength,buffer.maxByteLength,buffer.growable,alias.growable);
 buffer.grow(16);
 console.log('grown',buffer.byteLength,alias.byteLength,tracking.length,fixed.length,offset.length,view.byteLength,fixedView.byteLength,Array.from(aliasView).join(','));
 alias.grow(24);aliasView[20]=42;
 console.log('alias',buffer.byteLength,tracking.length,tracking[20],view.getUint8(20));
 const iterator=tracking.values(); console.log('iterator',iterator.next().value);
 buffer.grow(32);let count=1;for(const value of iterator) count++;
 console.log('iteration',count,tracking.length,tracking[31]);
 const slice=buffer.slice(0,4);console.log('slice',slice.growable,slice.maxByteLength,Array.from(new Uint8Array(slice)).join(','));
 for(const size of [32,31,33,-1,Infinity]) {
  try {buffer.grow(size);console.log('grow',size,buffer.byteLength);}catch(error){console.log('grow',size,(error as Error).name,buffer.byteLength);}
 }
 for(const [size,max] of [[0,0],[4,4],[5,4],[-1,8]]) {
  try {const value=new SharedArrayBuffer(size,{maxByteLength:max});console.log('construct',value.byteLength,value.maxByteLength,value.growable);}catch(error){console.log('construct',(error as Error).name);}
 }
 let calls=0;const native=new SharedArrayBuffer(4);
 try {(native as any).grow({valueOf(){calls++;return 4;}});}catch(error){console.log('fixed',(error as Error).name,calls);}
 const reentrant=new SharedArrayBuffer(4,{maxByteLength:16});
 try {(reentrant as any).grow({valueOf(){reentrant.grow(12);return 8;}});}catch(error){console.log('reentrant',(error as Error).name,reentrant.byteLength);}
 const atomicBuffer=new SharedArrayBuffer(4,{maxByteLength:16}), atomic=new Int32Array(atomicBuffer);
 const waiting=Atomics.waitAsync(atomic,0,0);atomicBuffer.grow(16);
 Atomics.store(atomic,3,99);console.log('atomic',atomic.length,Atomics.load(atomic,3),Atomics.notify(new Int32Array(structuredClone(atomicBuffer)),0,1));
 console.log('wait',waiting.async,await waiting.value);
}
run();
