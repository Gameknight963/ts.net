async function run():Promise<void>{
 const data=new TextEncoder().encode('OAEP test');
 for(const hash of ['SHA-1','SHA-256','SHA-384','SHA-512']){
  const pair=await crypto.subtle.generateKey({name:'RSA-OAEP',modulusLength:2048,publicExponent:new Uint8Array([1,0,1]),hash},true,['encrypt','decrypt','wrapKey','unwrapKey']);
  console.log('usages',pair.publicKey.usages.join(','),pair.privateKey.usages.join(','));
  const encrypted=await crypto.subtle.encrypt('RSA-OAEP',pair.publicKey,data);
  console.log('roundtrip',hash,encrypted.byteLength,new TextDecoder().decode(await crypto.subtle.decrypt('RSA-OAEP',pair.privateKey,encrypted)));
  const explicit=await crypto.subtle.encrypt({name:'RSA-OAEP',label:new Uint8Array(0)},pair.publicKey,new Uint8Array(0));
  console.log('empty',(await crypto.subtle.decrypt({name:'RSA-OAEP',label:new Uint8Array(0)},pair.privateKey,explicit)).byteLength);
  for(const format of ['spki','jwk'] as const){
   const exported=await crypto.subtle.exportKey(format as any,pair.publicKey);
   const key=await crypto.subtle.importKey(format as any,exported as any,{name:'RSA-OAEP',hash},true,['encrypt']);
   const bytes=await crypto.subtle.encrypt('RSA-OAEP',key,data);
   console.log('public',format,new TextDecoder().decode(await crypto.subtle.decrypt('RSA-OAEP',pair.privateKey,bytes)));
  }
  for(const format of ['pkcs8','jwk'] as const){
   const exported=await crypto.subtle.exportKey(format as any,pair.privateKey);
   const key=await crypto.subtle.importKey(format as any,exported as any,{name:'RSA-OAEP',hash},true,['decrypt']);
   console.log('private',format,new TextDecoder().decode(await crypto.subtle.decrypt('RSA-OAEP',key,encrypted)));
  }
  const clone=structuredClone(pair);
  console.log('clone',new TextDecoder().decode(await crypto.subtle.decrypt('RSA-OAEP',clone.privateKey,await crypto.subtle.encrypt('RSA-OAEP',clone.publicKey,data))));
  console.log('algorithm',(await crypto.subtle.exportKey('jwk',pair.publicKey)).alg,pair.publicKey.algorithm.name);
 }
}
run();
