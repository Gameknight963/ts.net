export {};
async function main():Promise<void>{
 for(const bits of [128,192,256]){
  const bytes=new Uint8Array(bits/8);for(let i=0;i<bytes.length;i++)bytes[i]=i;
  const key=await crypto.subtle.importKey("raw",bytes,"AES-GCM",true,["encrypt","decrypt"]);
  console.log("key",key instanceof CryptoKey,key.algorithm.name,(key.algorithm as AesKeyAlgorithm).length,key.usages.join(","));
  for(const tagLength of [96,104,112,120,128])for(const length of [0,1,16,33]){
   const data=new Uint8Array(length);for(let i=0;i<length;i++)data[i]=i+1;
   const params={name:"AES-GCM",iv:new Uint8Array(12),additionalData:new Uint8Array([4,5,6]),tagLength};
   const encrypted=await crypto.subtle.encrypt(params,key,data);
   console.log("cipher",bits,tagLength,length,(new Uint8Array(encrypted) as any).toHex());
   console.log("plain",(new Uint8Array(await crypto.subtle.decrypt(params,key,encrypted)) as any).toHex());
   const bad=new Uint8Array(encrypted.slice(0));bad[bad.length-1]^=1;
   try{await crypto.subtle.decrypt(params,key,bad);console.log("bad accepted");}catch(error){console.log("tampered",(error as Error).name);}
  }
  const jwk=await crypto.subtle.exportKey("jwk",key);console.log("jwk",jwk.kty,jwk.alg,jwk.k,jwk.ext,jwk.key_ops?.join(","));
  const roundtrip=await crypto.subtle.importKey("jwk",jwk,"AES-GCM",false,["decrypt"]);
  const params={name:"AES-GCM",iv:new Uint8Array(12)};
  console.log("jwk round trip",new Uint8Array(await crypto.subtle.decrypt(params,roundtrip,await crypto.subtle.encrypt(params,key,new Uint8Array([9]))))[0]);
  const generated=await crypto.subtle.generateKey({name:"AES-GCM",length:bits},true,["encrypt","decrypt"]);
  console.log("generated",(generated.algorithm as AesKeyAlgorithm).length,(await crypto.subtle.exportKey("raw",generated)).byteLength);
 }
 const key=await crypto.subtle.importKey("raw",new Uint8Array(16),"AES-GCM",false,["encrypt"]);
 const params={name:"AES-GCM",iv:new Uint8Array(12)};
 console.log("zero vector",(new Uint8Array(await crypto.subtle.encrypt(params,key,new Uint8Array(16))) as any).toHex());
 for(const [label,run] of [
  ["permission",()=>crypto.subtle.decrypt(params,key,new Uint8Array(16))],
  ["iv",()=>crypto.subtle.encrypt({name:"AES-GCM",iv:new Uint8Array(8)},key,new Uint8Array())],
  ["tag",()=>crypto.subtle.encrypt({...params,tagLength:80},key,new Uint8Array())],
  ["length",()=>crypto.subtle.generateKey({name:"AES-GCM",length:100},true,["encrypt"])],
  ["raw",()=>crypto.subtle.importKey("raw",new Uint8Array(17),"AES-GCM",true,["encrypt"])],
  ["usage",()=>crypto.subtle.generateKey({name:"AES-GCM",length:128},true,["sign"])],
 ] as [string,()=>Promise<unknown>][]){try{await run();console.log(label,"accepted");}catch(error){console.log(label,(error as Error).name);}}
}
main().catch(error=>console.log("failure",String(error)));
