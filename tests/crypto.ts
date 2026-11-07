export {};
async function main():Promise<void>{
 console.log("types",crypto instanceof Crypto,crypto.subtle instanceof SubtleCrypto,crypto.subtle===crypto.subtle,Object.prototype.toString.call(crypto),Object.prototype.toString.call(crypto.subtle));
 for(const array of [new Uint8Array(32),new Int8Array(16),new Uint8ClampedArray(8),new Uint16Array(8),new Int16Array(8),new Uint32Array(8),new Int32Array(8),new BigInt64Array(4),new BigUint64Array(4)])console.log("random",array.constructor.name,crypto.getRandomValues(array)===array,array.byteLength);
 const storage=new Uint8Array(20);storage.fill(7);crypto.getRandomValues(storage.subarray(4,16));console.log("bounds",storage[0],storage[3],storage[16],storage[19]);
 const maximum=new Uint8Array(65536);console.log("quota edge",crypto.getRandomValues(maximum)===maximum);
 const uuid=crypto.randomUUID();console.log("uuid",/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/.test(uuid));
 for(const input of [undefined,{},new Float32Array(2),new DataView(new ArrayBuffer(2)),new Uint8Array(65537)])try{crypto.getRandomValues(input as any);}catch(error){console.log("random error",(error as DOMException).name,(error as DOMException).code);}
 for(const name of ["SHA-1","SHA-256","SHA-384","SHA-512","sha-256"]){for(const text of ["","abc","hello world"]){const digest=await crypto.subtle.digest(name,new TextEncoder().encode(text));console.log("digest",name,text,(new Uint8Array(digest) as any).toHex());}}
 const input=new Uint8Array([97,98,99]);const promise=crypto.subtle.digest({name:"SHA-256"},new DataView(input.buffer));input.fill(0);console.log("snapshot",(new Uint8Array(await promise) as any).toHex());
 for(const algorithm of ["missing",{},null])try{await crypto.subtle.digest(algorithm as any,new Uint8Array());}catch(error){console.log("digest error",(error as Error).name);}
 const offset=new Uint8Array([0,97,98,99,0]);console.log("view digest",(new Uint8Array(await crypto.subtle.digest("SHA-256",new DataView(offset.buffer,1,3))) as any).toHex());
 class Derived extends Crypto{};console.log("native base",typeof Derived);try{new Derived();}catch(error){console.log("derived error",(error as Error).name);}
 for(const C of [Crypto,SubtleCrypto])try{new C();}catch(error){console.log("constructor",(error as Error).name);}
}
main().catch(error=>console.log("failure",String(error)));
