export {};
async function main():Promise<void>{
 const iv=new Uint8Array(16);for(let i=0;i<iv.length;i++)iv[i]=i;
 const params={name:"AES-CBC",iv};
 for(const bits of [128,192,256]){
  const material=new Uint8Array(bits/8);for(let i=0;i<material.length;i++)material[i]=i;
  const key=await crypto.subtle.importKey("raw",material,"AES-CBC",true,["encrypt","decrypt","wrapKey","unwrapKey"]);
  console.log("key",key instanceof CryptoKey,key.algorithm.name,(key.algorithm as AesKeyAlgorithm).length,key.usages.join(","));
  for(const length of [0,1,15,16,17,31,32,33]){
   const data=new Uint8Array(length);for(let i=0;i<length;i++)data[i]=i+1;
   const ciphertext=await crypto.subtle.encrypt(params,key,data);
   console.log("encrypted",bits,length,(new Uint8Array(ciphertext) as any).toHex());
   console.log("decrypted",(new Uint8Array(await crypto.subtle.decrypt(params,key,ciphertext)) as any).toHex());
  }
  const jwk=await crypto.subtle.exportKey("jwk",key);console.log("jwk",jwk.alg,jwk.k,jwk.ext,jwk.key_ops?.join(","));
  const imported=await crypto.subtle.importKey("jwk",jwk,"AES-CBC",false,["decrypt"]);
  console.log("JWK round trip",(new Uint8Array(await crypto.subtle.decrypt(params,imported,await crypto.subtle.encrypt(params,key,new Uint8Array([1,2,3])))) as any).toHex());
  for(const format of ["raw","jwk"] as const){
   const wrapped=await crypto.subtle.wrapKey(format,key,key,params);
   const restored=await crypto.subtle.unwrapKey(format,wrapped,key,params,"AES-CBC",true,["encrypt"]);
   console.log("wrapped",format,wrapped.byteLength,(new Uint8Array(await crypto.subtle.exportKey("raw",restored)) as any).toHex());
  }
  const generated=await crypto.subtle.generateKey({name:"AES-CBC",length:bits},true,["encrypt"]);
  console.log("generated",generated.algorithm.name,(generated.algorithm as AesKeyAlgorithm).length,(await crypto.subtle.exportKey("raw",generated)).byteLength);
  const padded=new Uint8Array(await crypto.subtle.encrypt(params,key,new Uint8Array(16)));padded[15]^=16;
  try{await crypto.subtle.decrypt(params,key,padded);console.log("bad padding accepted");}catch(error){console.log("padding",(error as Error).name);}
  const cloned=structuredClone(key);console.log("clone",cloned!==key,cloned.algorithm.name,(cloned.algorithm as AesKeyAlgorithm).length);
 }
 const key=await crypto.subtle.importKey("raw",new Uint8Array(16),"AES-CBC",true,["encrypt","decrypt"]);
 for(const [label,run] of [
  ["IV",()=>crypto.subtle.encrypt({name:"AES-CBC",iv:new Uint8Array(12)},key,new Uint8Array())],
  ["short",()=>crypto.subtle.decrypt(params,key,new Uint8Array(15))],
  ["empty cipher",()=>crypto.subtle.decrypt(params,key,new Uint8Array())],
  ["wrong algorithm",()=>crypto.subtle.encrypt({name:"AES-GCM",iv:new Uint8Array(12)},key,new Uint8Array())],
  ["wrong JWK",()=>crypto.subtle.importKey("jwk",{kty:"oct",k:"AAAAAAAAAAAAAAAAAAAAAA",alg:"A128GCM"},"AES-CBC",true,["encrypt"])],
 ] as [string,()=>Promise<unknown>][]){try{await run();console.log(label,"accepted");}catch(error){console.log(label,(error as Error).name);}}
 const base=await crypto.subtle.importKey("raw",new Uint8Array([1,2,3]),"PBKDF2",false,["deriveKey"]);
 const derived=await crypto.subtle.deriveKey({name:"PBKDF2",hash:"SHA-256",salt:new Uint8Array([4,5,6]),iterations:10},base,{name:"AES-CBC",length:256},true,["encrypt"]);
 console.log("derived",derived.algorithm.name,(new Uint8Array(await crypto.subtle.exportKey("raw",derived)) as any).toHex());
}
main().catch(error=>console.log("failure",String(error)));
