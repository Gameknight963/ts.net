export {};
async function main():Promise<void>{
 class Derived extends CryptoKey{};console.log("key subclass",typeof Derived);
 for(const Constructor of [CryptoKey,Derived])try{new Constructor();}catch(error){console.log("key constructor",(error as Error).name);}
 const bytes=new TextEncoder().encode("key"),data=new TextEncoder().encode("The quick brown fox jumps over the lazy dog");
 for(const hash of ["SHA-1","SHA-256","SHA-384","SHA-512"]){
  const key=await crypto.subtle.importKey("raw",bytes,{name:"hmac",hash},true,["sign","verify"]);
  console.log("key",key instanceof CryptoKey,key.type,key.extractable,key.algorithm.name,(key.algorithm as HmacKeyAlgorithm).hash.name,(key.algorithm as HmacKeyAlgorithm).length,key.usages.join(","),Object.prototype.toString.call(key));
  const signature=await crypto.subtle.sign("HMAC",key,data);
  console.log("signature",hash,(new Uint8Array(signature) as any).toHex(),await crypto.subtle.verify("HMAC",key,signature,data));
  const bad=new Uint8Array(signature.slice(0));bad[0]^=1;
  console.log("reject",await crypto.subtle.verify("HMAC",key,bad,data),await crypto.subtle.verify("HMAC",key,new Uint8Array(),data));
  console.log("export",(new Uint8Array(await crypto.subtle.exportKey("raw",key)) as any).toHex());
  const generated=await crypto.subtle.generateKey({name:"HMAC",hash},true,["verify","sign"]);
  const exported=await crypto.subtle.exportKey("raw",generated);const imported=await crypto.subtle.importKey("raw",exported,{name:"HMAC",hash},false,["verify"]);
  console.log("generated",(generated.algorithm as HmacKeyAlgorithm).length,exported.byteLength,generated.usages.join(","),await crypto.subtle.verify("HMAC",imported,await crypto.subtle.sign("HMAC",generated,data),data));
 }
 const storage=new Uint8Array([0,107,101,121,0]);const promise=crypto.subtle.importKey("raw",new DataView(storage.buffer,1,3),{name:"HMAC",hash:"SHA-256"},true,["sign"]);storage.fill(0);const key=await promise;
 console.log("snapshot",(new Uint8Array(await crypto.subtle.exportKey("raw",key)) as any).toHex());
 const protectedKey=await crypto.subtle.importKey("raw",bytes,{name:"HMAC",hash:"SHA-256"},false,["sign"]);
 const failures:[string,()=>Promise<unknown>][]=[
  ["export private",()=>crypto.subtle.exportKey("raw",protectedKey)],
  ["permission",()=>crypto.subtle.verify("HMAC",protectedKey,new Uint8Array(32),data)],
  ["empty key",()=>crypto.subtle.importKey("raw",new Uint8Array(),{name:"HMAC",hash:"SHA-256"},false,["sign"])],
  ["empty usages",()=>crypto.subtle.generateKey({name:"HMAC",hash:"SHA-256"},false,[])],
  ["wrong usages",()=>crypto.subtle.generateKey({name:"HMAC",hash:"SHA-256"},false,["encrypt"])],
  ["hash",()=>crypto.subtle.generateKey({name:"HMAC",hash:"MD5"},false,["sign"])],
  ["length",()=>crypto.subtle.importKey("raw",bytes,{name:"HMAC",hash:"SHA-256",length:32},true,["sign"])],
  ["missing hash",()=>crypto.subtle.generateKey({name:"HMAC"},false,["sign"])],
  ["key brand",()=>crypto.subtle.sign("HMAC",{} as CryptoKey,data)],
 ];
 for(const [label,run] of failures)try{await run();console.log(label,"unexpected success");}catch(error){console.log(label,(error as Error).name);}
}
main().catch(error=>console.log("failure",String(error)));
